(*
 * The MIT License (MIT)
 * Copyright (c) Arturo Rodriguez All rights reserved.
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *)

namespace QuantApp.Engine

open System
open System.IO
open System.IO.Compression

open System.Security.Cryptography
open System.Text
open System.Reflection

open FSharp.Compiler.SourceCodeServices

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.CSharp
open Microsoft.CodeAnalysis.VisualBasic

open Jint.Native

open QuantApp.Kernel

open Python.Runtime
open QuantApp.Kernel.JVM

open System.Xml
open System.Xml.Linq
open System.Net

open FSharp.Interop.Dynamic

type JsWrapper =

    val _engine : Jint.Engine
    val mutable private monitorInitialize : obj

    new(engine : Jint.Engine) = { _engine = engine; monitorInitialize = Object() }


    member this.Wrapper (func : Func<Jint.Native.JsValue,Jint.Native.JsValue[],Jint.Native.JsValue>) (parameters : obj[]) : obj =
        lock (this.monitorInitialize) (fun () ->
            try
                let res =
                    func.Invoke(
                        Jint.Native.JsValue.Undefined,
                        if parameters |> isNull then
                            null
                        else
                            parameters 
                            |> Array.map(fun x -> 
                                if x.ToString().StartsWith("{\"") then
                                    let objr = x :> obj
                                    let res = Jint.Native.Json.JsonParser(this._engine).Parse(objr.ToString()) :> JsValue
                                    res                                
                                else
                                    let res = Jint.Native.JsValue.FromObject(this._engine, x)
                                    res
                                )
                            )
                
                if res = Jint.Native.JsValue.Undefined then
                    null
                else
                    res.ToObject() :> obj
            with
            | ex -> 
                "JS Wrapper Error: " + ex.ToString() |> Console.WriteLine
                null
            )

    member this.Load (name, jsFunc) =
        Utils.SetFunction(name, (QuantApp.Engine.Load(fun data -> this.Wrapper jsFunc data |> ignore)))

    member this.Callback(name, jsFunc) =
        Utils.SetFunction(name, (QuantApp.Kernel.MCallback(fun id data -> this.Wrapper jsFunc [|(id :> obj); data|] |> ignore)))

    member this.Body(name, jsFunc) =
        Utils.SetFunction(name, (QuantApp.Engine.Body(fun data -> this.Wrapper jsFunc [|data|])))

    member this.Job(name, jsFunc) =
        Utils.SetFunction(name, (QuantApp.Engine.Job(fun date data -> this.Wrapper jsFunc [|(date :> obj); data|] |> ignore)))


    member this.Predicate(jsFunc) =
        System.Func<obj, bool>(fun data -> 
            let res = this.Wrapper jsFunc [|data|]
            if res |> isNull then
                false
            else
                res :?> bool
            )


module Code =
    let setPythonOut = 
        "import sys\n" +
        "from System import Console\n" +
        "class output(object):\n" +
        "   def write(self, msg):\n" +
        "       Console.Write(msg)\n" +
        "   def writelines(self, msgs):\n" +
        "       for msg in msgs:\n" +
        "           Console.Write(msg)\n" +
        "   def flush(self):\n" +
        "       pass\n" +
        "   def close(self):\n" +
        "       pass\n" +
        "sys.stdout = sys.stderr = output()"

    let setPythonImportPath (path : string) = 
        let path = path.Replace("\\", "\\\\")
        "import sys\n" +
        "if not '" + path + "' in sys.path:\n" +
        "   sys.path.append('" + path + "')"

    let GetMd5Hash (code: string) = 
        let md5 = MD5.Create()
        let codeBytes = Encoding.UTF8.GetBytes(code)
        let hash = md5.ComputeHash(codeBytes)
        let sb = StringBuilder()
        for b in hash do sb.Append(b.ToString("x2")) |> ignore done
        sb.ToString()
    let CompiledAssemblies = System.Collections.Concurrent.ConcurrentDictionary<string, Assembly>()
    let CompiledPackages = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let CompiledBase = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let LoadedNuGets = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let LoadedPips = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let LoadedJars = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let CompiledPythonModules = System.Collections.Concurrent.ConcurrentDictionary<string, PyObject>()
    let CompiledPythonModulesName = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let CompiledPythonModulesNameHash = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let CompiledJVMClasses = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let CompiledJVMBaseClasses = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let filePaths = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let listeningPaths = System.Collections.Concurrent.ConcurrentDictionary<string, FileSystemWatcher>()
    let lastBuilt = System.Collections.Concurrent.ConcurrentDictionary<string, string>()

    let InstallJar (url : String) : unit =
        let wc = new WebClient()
        let uri = url |> Uri
        let fileName = uri.LocalPath |> Path.GetFileName
        let fileName = "mnt/jars/" + fileName
        let file = fileName |> FileInfo
        file.Directory.Create()

        if fileName |> File.Exists |> not then
            File.WriteAllBytes(fileName, wc.DownloadData(url)) |> ignore

        CompiledJVMBaseClasses.TryAdd(file.FullName, file.FullName)
        "Jar Installed: " + url |> Console.WriteLine

    let InstallNuGetAssembly (packageName : string) (packageVersion : string) : unit =

        let downloadPackage (masterName : string) version =
            let dict = System.Collections.Generic.Dictionary<string, string>()
            let rec _downloadPackage (name : string) version =
                try
                    if name = "NETStandard.Library" |> not && name = "Microsoft.NETCore.Platforms" |> not then
                        System.Reflection.Assembly.Load(name) |> ignore
                with
                | _ -> 
                    
                    let wc = new WebClient()
                    
                    let archive = 
                                "https://www.nuget.org/api/v2/package/" + name + "/" + version
                                |> wc.DownloadData
                                |> MemoryStream
                                |> ZipArchive
                                
                    //let files = Collections.Generic.Dictionary<string, string>()
                    let files =
                        [|
                            for entry in archive.Entries do
                                let fileName = entry.FullName
                                let entryStream = entry.Open()
                                if ".nuspec" |> fileName.EndsWith then
                                    let streamReader = StreamReader(entryStream)
                                    let content = streamReader.ReadToEnd()

                                    let doc = XmlDocument()
                                    doc.LoadXml(content)

                                    let frameworks (target : string) =
                                        let arr =
                                            [|
                                                for node in doc.GetElementsByTagName("group") do
                                                    let framework = node.Attributes.["targetFramework"].Value
                                                    let targetFramework = target
                                                    
                                                    if targetFramework |> framework.StartsWith then
                                                        let frameworkVersion = framework.Replace(target,"") |> Double.Parse
                                                        
                                                        for node in node.ChildNodes do
                                                            let id = node.Attributes.["id"].Value.ToString()
                                                            let version = node.Attributes.["version"].Value.ToString()
                                                            yield (targetFramework, frameworkVersion, id, version)

                                                if doc.GetElementsByTagName("group").Count = 0 then
                                                    for node in doc.GetElementsByTagName("dependency") do
                                                        let id = node.Attributes.["id"].Value.ToString()
                                                        let version = node.Attributes.["version"].Value.ToString()
                                                        yield (".NETStandard", 2.0, id, version)

                                            |]
                                        if arr |> Array.isEmpty then
                                            0.0, [||]
                                        else
                                            arr
                                            |> Array.groupBy(fun (framework, frameworkVersion, id, version) -> frameworkVersion)
                                            |> Array.sortBy(fun (frameworkVersion, _) -> -frameworkVersion)
                                            |> Array.head

                                    let _, netcoreapp = ".NETCoreApp" |> frameworks
                                    let _, netstandard = ".NETStandard" |> frameworks

                                    "Nuget Loading dependencies for: " + name + " " + version.ToString() |> Console.WriteLine
                                    
                                    if netcoreapp |> Array.isEmpty |> not then
                                        netcoreapp
                                        |> Array.iter(fun (framework, frameworkVersion, id, version) ->
                                            if dict.ContainsKey(id) |> not then
                                                dict.Add(id, id)
                                                _downloadPackage id version
                                        )
                                    elif netstandard |> Array.isEmpty |> not then
                                        netstandard
                                        |> Array.iter(fun (framework, frameworkVersion, id, version) ->
                                            if dict.ContainsKey(id) |> not then
                                                dict.Add(id, id)
                                                _downloadPackage id version
                                        )
                                elif "lib/" |> fileName.StartsWith then
                                    let framework = fileName.Replace("lib/", "")
                                    let framework = framework.Substring(0, framework.IndexOf("/"))
                                    let stream = MemoryStream()
                                    entryStream.CopyTo(stream)
                                    let content = stream.ToArray()
                                    yield (framework, fileName, content)
                        |]

                    let netcore =  
                        if files |> Array.isEmpty then [||] else 
                            files 
                            |> Array.filter(fun (framework, fileName, content) -> framework.StartsWith("netcoreapp") && fileName.EndsWith(".dll"))
                            |> Array.map(fun (framework, fileName, content) -> 
                                let version = Double.Parse(framework.Replace("netcoreapp",""))
                                (version, framework, fileName, content)
                                )
                            |> Array.sortBy(fun (version, framework, fileName, content) -> -version)
                    let netstandard =  
                        if files |> Array.isEmpty then [||] else 
                            files |> Array.filter(fun (framework, fileName, content) -> framework.StartsWith("netstandard") && fileName.EndsWith(".dll"))
                            |> Array.map(fun (framework, fileName, content) -> 
                                let version = Double.Parse(framework.Replace("netstandard",""))
                                (version, framework, fileName, content)
                                )
                            |> Array.sortBy(fun (version, framework, fileName, content) -> -version)


                    if netcore |> Array.isEmpty |> not then
                        let (version, framework, fileName, content) = netcore |> Array.head
                        let fileName = "mnt/nugets/" + masterName + "/" + fileName.Substring(fileName.LastIndexOf("/") + 1)
                        
                        (FileInfo(fileName)).Directory.Create()
                        File.WriteAllBytes(fileName, content)
                    elif netstandard |> Array.isEmpty |> not then
                        let (version, framework, fileName, content) = netstandard |> Array.head
                        let fileName = "mnt/nugets/" + masterName + "/" + fileName.Substring(fileName.LastIndexOf("/") + 1)
                        
                        (FileInfo(fileName)).Directory.Create()
                        File.WriteAllBytes(fileName, content)
                    ()

            let assembly = 
                try
                    System.Reflection.Assembly.Load(packageName)
                with
                | _ -> 
                    let filename = "mnt/nugets/" + masterName + "/" + masterName + ".dll"
                    if filename |> File.Exists |> not then
                        _downloadPackage masterName version

                    System.Reflection.Emit.AssemblyBuilder.LoadFrom("mnt/nugets/" + masterName + "/" + masterName + ".dll")
            assembly
        
        let assembly = downloadPackage packageName packageVersion
        "NuGet Loaded: " + packageName + " " + packageVersion.ToString() + " " + assembly.ToString() |> Console.WriteLine
        if M._compiledAssemblies.ContainsKey(packageName + packageVersion.ToString()) then
            M._compiledAssemblies.[packageName + packageVersion.ToString()] <- assembly
            M._compiledAssemblyNames.[packageName + packageVersion.ToString()] <- packageName + packageVersion.ToString()
        else
            M._compiledAssemblies.TryAdd(packageName + packageVersion.ToString(), assembly) |> ignore
            M._compiledAssemblyNames.TryAdd(packageName + packageVersion.ToString(), packageName + packageVersion.ToString()) |> ignore

    let installedPip = System.Collections.Concurrent.ConcurrentDictionary<string, string>()
    let InstallPip (packageName : string) : unit =
        if packageName |> installedPip.ContainsKey |> not then
            using (Py.GIL()) (fun _ -> 

                setPythonOut |> PythonEngine.RunSimpleString
                let code = 
                    "import subprocess \n" +
                    "try:\n" +
                    "   import " + (if packageName.IndexOf("=") > -1 then packageName.Substring(0, packageName.IndexOf("=")) else packageName) + "\n" +
                    "   print('Pip package: " + (if packageName.IndexOf("=") > -1 then packageName.Substring(0, packageName.IndexOf("=")) else packageName) + " exists.')\n" +
                    "except: \n" +
                    "   print('Installing Pip package: " + packageName + "...')\n" +
                    "   subprocess.check_call(['pip', 'install', '--target=/app/mnt/pip/', '" + packageName + "'])"
                code |> PythonEngine.Exec
            )
            installedPip.TryAdd(packageName, packageName) |> ignore

    let InitializeCodeTypes(types : Type[]) =

        using (Py.GIL()) (fun _ -> "/app/mnt/pip/" |> setPythonImportPath |> PythonEngine.RunSimpleString)
        
        let sys_names = System.Collections.Generic.Dictionary<string, int>()        
        types
        |> Array.iter(fun ttype ->
            let asm = System.Reflection.Assembly.GetAssembly(ttype)
            let types = 
                try
                    asm.GetTypes()
                with
                | :? ReflectionTypeLoadException as ex -> ex.Types |> Array.filter(fun x -> x |> isNull |> not)

            types
            |> Seq.iter(fun t -> 
                let name = t.ToString()
                
                if not(sys_names.ContainsKey(name)) then
                    sys_names.Add(name, 0)
                    if not(M._systemAssemblies.ContainsKey(name)) then
                        M._systemAssemblies.TryAdd(name, asm) |> ignore
                )
            )

        Assembly.GetEntryAssembly().GetReferencedAssemblies()
        |> Seq.append(Assembly.GetExecutingAssembly().GetReferencedAssemblies())
        |> Seq.append(Assembly.GetCallingAssembly().GetReferencedAssemblies())
        |> Seq.append([Assembly.GetEntryAssembly().GetName()])
        |> Seq.append([Assembly.GetExecutingAssembly().GetName()])
        |> Seq.append([Assembly.GetCallingAssembly().GetName()])
        |> Seq.iter(fun an -> 
            let name = an.ToString()
            
            if "QuantApp.Utils.Framework" |> name.Contains |> not then

                let asm = System.Reflection.Assembly.Load(name)
                let types =
                    try
                        asm.GetTypes()
                    with
                    | :? ReflectionTypeLoadException as ex -> ex.Types |> Array.filter(isNull >> not)//fun x -> x |> isNull |> not)

                types
                |> Seq.iter(fun t -> 
                    let name = t.ToString()
                    
                    if name |> sys_names.ContainsKey |> not then
                        sys_names.Add(name, 0)
                        if name |> M._systemAssemblies.ContainsKey |> not then
                            (name, asm) |> M._systemAssemblies.TryAdd |> ignore
                    ))

        try
            let fsi = "FSI-ASSEMBLY" |> System.Reflection.Assembly.Load
            let fsi = if fsi |> isNull then "fsiAnyCpu" |> System.Reflection.Assembly.Load else fsi

            if fsi |> isNull |> not then
                fsi.GetTypes()
                |> Seq.iter(fun t -> 
                    let name = 
                        let n = t.ToString()
                        if "FSI_" |> n.StartsWith then
                            n.IndexOf(".") + 1 |> n.Substring
                        else
                            n

                    if name |> M._systemAssemblies.ContainsKey |> not then
                            (name, fsi) |> M._systemAssemblies.TryAdd |> ignore
                            (name, t.ToString()) |> M._systemAssemblyNames.TryAdd |> ignore
                    else
                        M._systemAssemblies.[name] <- fsi
                        M._systemAssemblyNames.[name] <- t.ToString()
                    )
        with _ -> ()

        let compileExecute (saveDisk, execute) (codes_all : (string * string) list, functionName: string, parameters: obj []) =
            let codes = codes_all |> List.map(fun (name, code) -> name, code.Replace("open AQI.AQILabs.SecureWebClient",""))

            if saveDisk then
                codes 
                |> List.iter(fun (name, code) -> 
                    if name |> filePaths.ContainsKey then
                        let path = filePaths.[name]

                        let hashCode = code |> GetMd5Hash
                        let hashFile = File.ReadAllText(path) |> GetMd5Hash

                        if hashCode <> hashFile then File.WriteAllText(path, code)
                )

            let sbuilder = StringBuilder()
            let resdb = Collections.Generic.List<string * obj * obj>()
            // let expdb = Collections.Generic.List<string * obj>()
            
            try
                let csFlag = "//cs"
                let vbFlag = "'vb"
                let fsFlag = "//fs"
                let jsFlag = "//js"
                let pyFlag = "#py"
                let jvFlag = "//java"
                let scFlag = "//scala"

                let documentation = 
                    if functionName = "?" || functionName = "??" then
                        codes_all
                        |> List.map(fun (name, code) ->
                            try
                                let codes = code.Split([|"\r\n"; "\r"; "\n"|], StringSplitOptions.None)
                                let xmlString = codes |> Array.fold(fun acc line -> acc + if line.TrimStart().StartsWith("///") || line.TrimStart().StartsWith("###") || line.TrimStart().StartsWith("'''") then line.Replace("///", "").Replace("###", "").Replace("'''", "") else "") ""
                                let doc = XDocument.Parse("<root>" + xmlString + "</root>")

                                let xn s = XName.Get(s)
                                let info = doc.Element(xn "root").Element(xn "info")

                                let info_title = if info |> isNull || info.Element(xn "title") |> isNull then name else info.Element(xn "title").Value
                                let info_version = if info |> isNull || info.Attribute(xn "version") |> isNull then "0.0.1" else info.Attribute(xn "version").Value;
                                let info_description = if info |> isNull || info.Element(xn "description") |> isNull then "no description found" else info.Element(xn "description").Value
                                let info_termsOfService = if info |> isNull || info.Element(xn "termsOfService") |> isNull then "" else info.Element(xn "termsOfService").Attribute(xn "url").Value

                                let info_contact = if info |> isNull then null else info.Element(xn "contact")
                                let info_contact_name = if info_contact |> isNull || info_contact.Attribute(xn "name") |> isNull then "" else info_contact.Attribute(xn "name").Value
                                let info_contact_url = if info_contact |> isNull || info_contact.Attribute(xn "url") |> isNull then "" else info_contact.Attribute(xn "url").Value
                                let info_contact_email = if info_contact |> isNull || info_contact.Attribute(xn "email") |> isNull then "" else info_contact.Attribute(xn "email").Value

                                let info_license = if info |> isNull then null else info.Element(xn "license")
                                let info_license_name = if info_license |> isNull || info_license.Attribute(xn "name") |> isNull then "" else info_license.Attribute(xn "name").Value
                                let info_license_url = if info_license |> isNull || info_license.Attribute(xn "url") |> isNull then "" else info_license.Attribute(xn "url").Value

                                let info_pair = (
                                    "#info", 
                                    {| 
                                        Title = info_title; 
                                        Version = info_version; 
                                        Description = info_description; 
                                        TermsOfService = info_termsOfService; 
                                        Contact = {| Name = info_contact_name; URL = info_contact_url; Email = info_contact_email |};
                                        License = {| Name = info_license_name; URL = info_license_url |};
                                    |} :> obj,
                                    null)
                                info_pair |> resdb.Add

                                let apis = doc.Elements(xn "root").Elements(xn "api")

                                name.ToLower(),
                                apis 
                                |> Seq.map(fun api -> 
                                    let _functionName = if api.Attribute(xn "name") |> isNull then "" else api.Attribute(xn "name").Value;
                                    let description = if api.Element(xn "description") |> isNull then "" else api.Element(xn "description").Value
                                    let returns = if api.Element(xn "returns") |> isNull then "" else api.Element(xn "returns").Value

                                    let parametersDoc = api.Elements(xn "param")
                                    let parameters =
                                        if parametersDoc |> isNull then 
                                            Seq.empty
                                        else 
                                            parametersDoc 
                                            |> Seq.map(fun parameter -> 
                                                let parName = if parameter.Attribute(xn "name") |> isNull then "" else parameter.Attribute(xn "name").Value
                                                let parType = if parameter.Attribute(xn "type") |> isNull then null else parameter.Attribute(xn "type").Value
                                                let parValue = parameter.Value

                                                {|
                                                    Name = parName;
                                                    Description = parValue;
                                                    Type = parType
                                                |})

                                    let permissionsDoc = if api.Element(xn "permissions") |> isNull then null elif api.Element(xn "permissions").Elements(xn "group") |> isNull then null else api.Element(xn "permissions").Elements(xn "group")
                                    let permissions =
                                        if permissionsDoc |> isNull then 
                                            Seq.empty
                                        else 
                                            permissionsDoc 
                                            |> Seq.map(fun group -> 
                                                
                                                let groupID = if group.Attribute(xn "id") |> isNull then "" else group.Attribute(xn "id").Value
                                                let cost = if group.Attribute(xn "cost") |> isNull then null else group.Attribute(xn "cost").Value
                                                let currency = if group.Attribute(xn "currency") |> isNull then null else group.Attribute(xn "currency").Value
                                                let perType = if group.Attribute(xn "type") |> isNull then null else group.Attribute(xn "type").Value
                                                let accessTypeStr = if group.Attribute(xn "permission") |> isNull then "Denied" else group.Attribute(xn "permission").Value

                                                let mutable accessType = AccessType.Denied

                                                Enum.TryParse<AccessType>(accessTypeStr, true, &accessType)
                                                
                                                {|
                                                    GroupID = groupID;
                                                    Cost = if cost |> isNull then 0.0 else Double.Parse(cost);
                                                    Currency = currency;
                                                    Type = perType;
                                                    Access = accessType
                                                |})

                                    let api_pair = 
                                        _functionName,
                                        {|
                                            Name = _functionName;
                                            Description = description;
                                            Returns = returns;
                                            Parameters = parameters;
                                            Permissions = permissions
                                        |} :> obj
                                    if functionName = "??" then
                                        let _n, _v = api_pair
                                        (_n, _v, null) |> resdb.Add
                                    api_pair
                                    )
                                
                                // |> Seq.append(seq { "#info", info.ToString() :> obj})
                                |> Map.ofSeq
                            with _ ->
                                name.ToLower(), Seq.empty |> Map.ofSeq
                            )
                        |> List.toSeq |> Map.ofSeq
                    else
                        Seq.empty |> Map.ofSeq

                if functionName = "??" |> not then    
                    let libs() =
                        #if NETCOREAPP3_1
                        let sysDir_base = Path.GetDirectoryName(@"ref/netcoreapp3.1/")
                        #endif

                        #if NET461
                        let sysDir_base = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                        #endif

                        let dir_sys = DirectoryInfo(sysDir_base)
                        let files_sys =  dir_sys.GetFiles()
                        
                        let fsDir_base = Path.GetDirectoryName(Uri.UnescapeDataString((UriBuilder(Assembly.GetAssembly(typeof<List<int>>).CodeBase)).Path))// + "/publish"
                        
                        let libDir_base = Path.GetDirectoryName(Uri.UnescapeDataString((UriBuilder(Assembly.GetAssembly(typeof<QuantApp.Kernel.M>).CodeBase)).Path))// + "/publish"
                        let dir_lib = DirectoryInfo(libDir_base)
                        let files_lib =  dir_lib.GetFiles()

                        let listFiles assemblies =
                            let listFiles (assembly : Assembly) = try [Uri.UnescapeDataString((UriBuilder(assembly.CodeBase)).Path)] with | _ -> []
                            assemblies
                            |> List.map(fun assembly -> assembly |> listFiles)
                            |> List.fold(fun acc lst -> acc |> List.append(lst)) []
                            
                        
                        let (++) a b = System.IO.Path.Combine(a,b)
                        let sysPath nm = sysDir_base ++ nm
                        let fsPath nm = fsDir_base ++ nm
                        let libPath nm = libDir_base ++ nm


                        let assemblyList = 
                            M._compiledAssemblies.Values |> Seq.toList
                            |> List.append(
                                [
                                    typeof<Newtonsoft.Json.JsonConverter>
                                    typeof<System.Data.SqlClient.SqlDataReader>
                                    
                                    typeof<QuantApp.Kernel.M>
                                    typeof<QuantApp.Engine.F>

                                    typeof<Jint.Native.Array.ArrayConstructor>
                                    
                                    typeof<FSharp.Core.MeasureAttribute>
                                ]
                                |> List.map(Assembly.GetAssembly)
                            )

                        [|  
                            for r in (files_sys |> Array.map(fun f -> sysPath (f.ToString())) |> List.ofArray |> List.filter(fun f -> (f.Contains("netstandard.") || f.Contains("System.") || f.Contains("Microsoft.CSharp") || f.Contains("Microsoft.Win") || f.Contains("WindowsBase") || f.Contains("mscorlib") ) && not(f.Contains("System.Enterprise")) && f.EndsWith(".dll"))) do yield r //COMPILES!
                            for r in (listFiles assemblyList) do yield r
                            for r in (files_lib 
                                |> Array.map(fun f -> libPath (f.ToString()))
                                |> List.ofArray 
                                |> List.filter(fun f -> 
                                    let f = f |> Path.GetFileName
                                    (
                                        not(f.Contains("CoFlows.Server.")) && 
                                        not(f.Contains("_")) && 
                                        not(f.Contains("-")) && 
                                        not(f.Contains("clr")) && 
                                        not(f.Contains("dbgshim")) && 
                                        not(f.Contains("ucrtbase")) && 
                                        not(f.Contains("sni.dll")) && 
                                        not(f.Contains("sos.dll")) && 
                                        not(f.Contains("Microsoft.")) && 
                                        not(f.Contains("WindowsBase")) && 
                                        not(f.Contains("System.")) && 
                                        not(f.Contains("libuv")) && 
                                        not(f.Contains("FSharp.Data")) && 
                                        not(f.Contains("FSharp.Core")) && 
                                        not(f.Contains("hostfxr.dll")) && 
                                        not(f.Contains("mscor")) && 
                                        not(f.Contains("hostpolicy.dll")) && 
                                        // not(f.Contains("netstandard")) && 
                                        f.EndsWith(".dll")
                                    ))) do yield r
                        |]
                        |> Array.toSeq |> Seq.distinct |> Seq.toArray

                    let executeAssembly (a: Assembly) =
                        if execute then
                            try 
                                try
                                    a.GetTypes()
                                with
                                | :? ReflectionTypeLoadException as ex -> 
                                    ex.Types |> Array.filter(isNull >> not)
                                |> Array.iter(fun t ->
                                    
                                    let methods = t.GetMethods()
                                    for m in methods do
                                        try
                                            if m.IsStatic then
                                                let name = m.Name.Replace("get_","")
                                                try
                                                    let parameterInfo = m.GetParameters() |> Seq.toArray |> Array.map(fun pi -> pi.ParameterType)
                                                    if functionName |> String.IsNullOrWhiteSpace |> not && functionName = "?" |> not then
                                                        if functionName = name && (if parameterInfo |> isNull |> not && parameters |> isNull |> not then parameterInfo.Length = parameters.Length else true) then
                                                            let t0 = DateTime.Now
                                                            
                                                            let res = m.Invoke(
                                                                null, 
                                                                if parameters |> isNull then 
                                                                    null 
                                                                else 
                                                                    parameters 
                                                                    |> Array.mapi(fun i x -> 
                                                                        let ptype = parameterInfo.[i]
                                                                        
                                                                        match ptype with
                                                                        | p when p = typeof<System.Int32> -> System.Int32.Parse(x.ToString()) :> obj
                                                                        | p when p = typeof<System.Int64> -> System.Int64.Parse(x.ToString()) :> obj
                                                                        | p when p = typeof<System.Double> -> System.Double.Parse(x.ToString()) :> obj
                                                                        | p when p = typeof<System.Boolean> -> System.Boolean.Parse(x.ToString()) :> obj
                                                                        | p when p = typeof<System.DateTime> -> System.DateTime.Parse(x.ToString()) :> obj
                                                                        | p when p = typeof<System.Decimal> -> System.Decimal.Parse(x.ToString()) :> obj
                                                                        | p when p = typeof<System.Byte> -> System.Byte.Parse(x.ToString()) :> obj
                                                                        | p when p = typeof<System.Char> -> System.Char.Parse(x.ToString()) :> obj
                                                                        | p when p = typeof<System.Int16> -> System.Int16.Parse(x.ToString()) :> obj
                                                                        | _ -> x
                                                                        )
                                                                )
                                                            let pair = (name, res, null)
                                                            // "Executed: " + m.Name + " " + (DateTime.Now - t0).ToString() |> Console.WriteLine
                                                            pair |> resdb.Add
                                                    elif functionName = "?" then
                                                        // List all functions
                                                        let fname = t.ToString().ToLower()
                                                        let doc = if documentation.ContainsKey(fname) then documentation.[fname] elif documentation.ContainsKey(fname + ".cs") then documentation.[fname + ".cs"] elif documentation.ContainsKey(fname + ".fs") then documentation.[fname + ".fs"] elif documentation.ContainsKey(fname + ".vb") then documentation.[fname + ".vb"] else (Seq.empty |> Map.ofSeq)
                                                        
                                                        // let pair = (name, (if doc.ContainsKey(name) then doc.[name] else {| Name = ""; Summary = ""; Remarks = ""; Returns = ""; Parameters = Seq.empty |}) :> obj)
                                                        let pair = (name, (if doc.ContainsKey(name) then doc.[name] else null) :> obj, null)
                                                        pair |> resdb.Add

                                                    elif parameterInfo |> Array.isEmpty && t.Namespace |> isNull then
                                                        let res = m.Invoke(null, null)
                                                        let pair = (name, res, null)
                                                        pair |> resdb.Add
                                                with
                                                | ex ->
                                                    (name, null, if ex.InnerException |> isNull then (ex.Message.ToString() :> obj) else (ex.InnerException.ToString() :> obj))
                                                    |> resdb.Add
                                                    // |> expdb.Add
                                        with
                                        | ex -> 
                                            ex.ToString() |> sbuilder.AppendLine |> ignore
                                        )

                                if a.EntryPoint |> isNull |> not then
                                    a.EntryPoint.Invoke(null, null) |> ignore

                            with
                                | :? TargetInvocationException as tex -> "Execution failed with: " + (tex.InnerException.ToString()) |> sbuilder.AppendLine |> ignore
                                | ex -> "Execution cannot start, reason: " + ex.ToString() |> Console.WriteLine

                        resdb |> Seq.toList
                    
                    let compileFS (codes : (string * string) list) =
                        let str = codes |> List.fold(fun acc (name, code) -> acc + code) ""
                        let hash = str |> GetMd5Hash
                        if hash |> CompiledAssemblies.ContainsKey |> not then

                            let _compileFS (codes : (string * string) list) =
                                // let checker = FSharpChecker.Create()
                                let fn = Path.GetTempFileName()
                                let dllFile = Path.ChangeExtension(fn, ".dll")
                                let fsFiles =
                                    codes
                                    |> List.map(fun (name, code) ->
                                        let name = name.Replace("/","_")
                                        let fn = Path.GetTempFileName() + "-" + name
                                        let fsFile = Path.ChangeExtension(fn, ".fs")
                                        File.WriteAllText(fsFile, code)
                                        fsFile)

                                let args =
                                        [|  
                                            yield "";//fsc.exe";
                                            yield "--noframework";
                                            yield "--targetprofile:netcore" 
                                            yield "--optimize-" 
                                            yield "--target:library" 
                                            yield "-o"; yield dllFile; 
                                            yield "-a"; for r in fsFiles do yield " " + r
                                            for r in libs() do yield "-r:" + r
                                        |]
                                        |> Array.toSeq |> Seq.distinct |> Seq.toArray

                                let errors, exitCode, assembly = 
                                    let errors, exitCode = args |> FSharpChecker.Create().Compile |> Async.RunSynchronously

                                    let errors = errors |> Array.filter(fun x -> x.ToString().Contains("You must add a reference to assembly 'System.Private.CoreLib, Version=4.0.0.0") |> not)

                                                                        
                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(fun x -> sbuilder.AppendLine(x.ToString().Substring(x.ToString().LastIndexOf(".tmp-") + 5)) |> ignore)
                                    #if MONO_LINUX || MONO_OSX
                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(Console.WriteLine)
                                    #endif
                                    let assembly = System.Reflection.Emit.AssemblyBuilder.LoadFrom(dllFile)

                                    if codes |> List.isEmpty |> not && (snd codes.[0]).ToLower().Contains("namespace") then
                                        M._compiledAssemblies.TryAdd(hash, assembly) |> ignore
                                        try
                                            assembly.GetTypes()
                                            |> Seq.iter(fun t -> 
                                                let name = t.ToString()
                                                M._compiledAssemblyNames.[name] <- hash
                                                )
                                        with
                                        | _-> ()
                                    
                                    errors, exitCode, assembly

                                if errors |> Seq.filter(fun e -> e.ToString().Contains("error")) |> Seq.isEmpty && (assembly |> isNull |> not)  then
                                    assembly
                                else
                                    null

                            let assembly = codes |> _compileFS
                            if assembly |> isNull |> not then
                                (hash, assembly) |> CompiledAssemblies.TryAdd |> ignore
                                assembly |> executeAssembly |> ignore

                        else
                            CompiledAssemblies.[hash] |> executeAssembly |> ignore

                    let compileCS (codes : (string * string) list) =
                        let str = codes |> List.fold(fun acc (name, code) -> acc + code) ""
                        let hash = str |> GetMd5Hash
                        if hash |> CompiledAssemblies.ContainsKey |> not then

                            let _compileCS (codes : (string * string) list) =
                                let fn = Path.GetTempFileName()
                                let dllFile = Path.ChangeExtension(fn, ".dll")
                                                        
                                let asname = System.Guid.NewGuid().ToString()
                                let compilation = 
                                    let stree = 
                                        codes 
                                        |> List.map(fun (name, code) -> 
                                            CSharpSyntaxTree.ParseText(code, null, name, null)) 
                                        |> List.toArray
                                    let asms : MetadataReference[] = 
                                        [| 
                                            for lib in libs() do yield MetadataReference.CreateFromFile(Uri.UnescapeDataString(lib))
                                        |]

                                    CSharpCompilation.Create(asname, stree, asms, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

                                let emitResult = compilation.Emit(dllFile)
                                
                                let errors, exitCode, assembly = 
                                    let errors = emitResult.Diagnostics |> Seq.map(fun d -> d.ToString()) |> Seq.filter(fun x -> x.Contains("You must add a reference to assembly 'System.Private.CoreLib, Version=4.0.0.0") |> not)
                                    let exitCode = if emitResult.Success then 0 else 1
                                    let assembly = if emitResult.Success then System.Reflection.Emit.AssemblyBuilder.LoadFrom(dllFile) else null

                                    if emitResult.Success && (snd codes.[0]).ToLower().Contains("namespace") then 
                                        M._compiledAssemblies.TryAdd(hash, assembly) |> ignore
                                        try
                                            assembly.GetTypes()
                                            |> Seq.iter(fun t -> 
                                                let name = t.ToString()
                                                M._compiledAssemblyNames.[name] <- hash
                                                )
                                        with
                                        | _-> ()

                                    errors, exitCode, assembly

                                if exitCode = 0  then
                                    assembly
                                else
                                    #if MONO_LINUX || MONO_OSX
                                    errors |> Seq.iter(Console.WriteLine)
                                    #endif
                                    errors |> Seq.map(fun err -> err.ToString()) |> Seq.iter(sbuilder.AppendLine >> ignore)
                                    null

                            let assembly = codes |> _compileCS

                            if assembly |> isNull |> not then
                                (hash, assembly) |> CompiledAssemblies.TryAdd |> ignore
                                assembly |> executeAssembly |> ignore
                        else
                            CompiledAssemblies.[hash] |> executeAssembly |> ignore

                    let compileVB (codes : (string * string) list) =
                        let str = codes |> List.fold(fun acc (name, code) -> acc + code) ""
                        let hash = str |> GetMd5Hash
                        if hash |> CompiledAssemblies.ContainsKey |> not then

                            let _compileVB (codes : (string * string) list) =
                                let fn = Path.GetTempFileName()
                                let dllFile = Path.ChangeExtension(fn, ".dll")

                                let asname = System.Guid.NewGuid().ToString()
                                let compilation = 
                                    let stree = codes |> List.map(fun (name, code) -> VisualBasicSyntaxTree.ParseText(code, null, name, null)) |> List.toArray
                                    let asms : MetadataReference[] = 
                                        [| 
                                            for lib in libs() do yield MetadataReference.CreateFromFile(Uri.UnescapeDataString(lib))
                                        |]
                                    VisualBasicCompilation.Create(asname, stree, asms, VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

                                
                                let emitResult = compilation.Emit(dllFile)
                                
                                let errors, exitCode, assembly = 
                                    let errors = emitResult.Diagnostics |> Seq.map(fun d -> d.ToString())
                                    let exitCode = if emitResult.Success then 0 else 1
                                    let assembly = if emitResult.Success then System.Reflection.Emit.AssemblyBuilder.LoadFrom(dllFile) else null

                                    if emitResult.Success && (snd codes.[0]).ToLower().Contains("namespace") then 
                                        M._compiledAssemblies.TryAdd(hash, assembly) |> ignore
                                        try
                                            assembly.GetTypes()
                                            |> Seq.iter(fun t -> 
                                                let name = t.ToString()
                                                M._compiledAssemblyNames.[name] <- hash
                                                )
                                        with
                                        | _-> ()
                                        
                                    errors, exitCode, assembly

                                if exitCode = 0  then
                                    assembly
                                else
                                    #if MONO_LINUX || MONO_OSX
                                    errors |> Seq.iter(Console.WriteLine)
                                    #endif
                                    errors |> Seq.iter(fun err -> sbuilder.AppendLine(err.ToString()) |> ignore)
                                    null

                            let assembly = codes |> _compileVB

                            if assembly |> isNull |> not then
                                (hash, assembly) |> CompiledAssemblies.TryAdd |> ignore
                                assembly |> executeAssembly |> ignore
                        else
                            CompiledAssemblies.[hash]
                            |> executeAssembly |> ignore

                    let runPython (codes : (string * string) list) =
                        try
                            using (Py.GIL()) (fun _ ->
                                setPythonOut |> PythonEngine.RunSimpleString

                                let pyName, pyModule = 
                                    let pathTemp = Path.GetTempPath()

                                    pathTemp |> setPythonImportPath |> PythonEngine.RunSimpleString
                                    pathTemp + Path.DirectorySeparatorChar.ToString() + "Base" |> setPythonImportPath |> PythonEngine.RunSimpleString
                                    
                                    "/app/mnt/Base" |> setPythonImportPath |> PythonEngine.RunSimpleString

                                    let modules = 
                                        codes 
                                        |> List.map(fun (name : string, code : string) ->
                                            try
                                                let _name = name
                                                let code = code.Replace("from . ", "").Replace("from .. ", "").Replace("from ..", "from ").Replace("from .", "from ")
                                                let hash = code |> GetMd5Hash
                                            
                                                if hash |> CompiledPythonModules.ContainsKey && name |> CompiledPythonModulesNameHash.ContainsKey && CompiledPythonModulesNameHash.[name] = hash then
                                                    name, CompiledPythonModules.[hash]
                                                else

                                                    try
                                                        if name |> CompiledPythonModulesNameHash.ContainsKey then
                                                            let lastHash = CompiledPythonModulesNameHash.[name]
                                                            if lastHash |> CompiledPythonModules.ContainsKey then
                                                                let lastMod = CompiledPythonModules.[lastHash]
                                                                let name = "A" + lastHash + name
                                                                
                                                                let delCommand =
                                                                    "import sys \n" +
                                                                    "import " + name.Replace(".py","") + "  \n" +
                                                                    "del sys.modules['" + name + "'] \n" +
                                                                    "del " + name.Replace(".py","")

                                                                delCommand |> PythonEngine.Exec
                                                                CompiledPythonModules.TryRemove(lastHash) |> ignore
                                                    with 
                                                    | e -> e |> Console.WriteLine

                                                    let modFlag = "Base/" |> name.StartsWith |> not
                                                    CompiledPythonModulesNameHash.[name] <- hash

                                                    let name = (if modFlag then ("A" + hash) else "") + name

                                                    let pyFile = pathTemp + name.Replace("/", Path.DirectorySeparatorChar.ToString())

                                                    pyFile |> Path.GetDirectoryName |> Directory.CreateDirectory

                                                    File.WriteAllText(pyFile, code)


                                                    let modName = "<module '" + name + "' from '" + pyFile + "'>"

                                                    // Bug fix
                                                    // PythonEngine.ImportModule(name) should work.
                                                    // It works in Docker Mac and Windows (Alpine Linux Kernel).
                                                    // But randomly in Azure Ubuntu (older kernel version).
                                                    // This happens because Docker Engine on Linux leverages off
                                                    // the native kernel which is different to the VM used in Mac and Win.
                                                    
                                                    let namesplit = name.Split('/')
                                                    
                                                    if name.Contains("Base") && namesplit.Length > 2 then
                                                        
                                                        let pkgName = namesplit.[1]
                                                        pkgName |> InstallPip
                                                        _name, null
                                                    else
                                                    
                                                        let pyMod = PythonEngine.CompileToModule(name, code, pyFile)
                                                        if pyMod |> isNull then 
                                                            "Error loading: " + name |> Console.WriteLine
                                                            _name, null 
                                                        else
                                                            (hash, pyMod) |> CompiledPythonModules.TryAdd
                                                            if modFlag |> not then (modName, modName) |> CompiledPythonModulesName.TryAdd |> ignore 
                                                            _name, pyMod
                                            with
                                            | ex -> 
                                                ex |> Console.WriteLine
                                                if "required positional argument" |> ex.Message.Contains |> not then
                                                    ex.Message |> sbuilder.AppendLine |> ignore
                                                "", null
                                        )

                                    modules |> List.last

                                if pyModule |> isNull |> not then
                                    let moduleName = pyModule.ToString()
                                    if execute then
                                        let rec obje_func (func : PyObject, name : string) cls = 
                                            if cls = "type" || cls = "CLR.CLR Metatype" || cls = "CLR.ModuleObject" || cls = "module" then
                                                null
                                            else
                                                let func_str = func.ToString()
                                                let inner_call =
                                                    if 
                                                        func.IsCallable() |> not
                                                        || (cls |> M._systemAssemblies.ContainsKey)
                                                        || (cls |> M._compiledAssemblyNames.ContainsKey)
                                                        || (moduleName |> CompiledPythonModulesName.ContainsKey)
                                                    then
                                                        null
                                                    else
                                                        try
                                                            let par = if parameters |> isNull then [||] else parameters |> Array.map(fun x -> PyString(x.ToString()) :> PyObject )
                                                            let result = pyModule.InvokeMethod(name, par)

                                                            let cls = result.GetPythonType().ToString().Replace("<class '","").Replace("'>","")
                                                            if cls |> M._systemAssemblies.ContainsKey then
                                                                let ttype = M._systemAssemblies.[cls].GetType(cls)

                                                                result.AsManagedObject(ttype)

                                                            elif cls |> M._compiledAssemblyNames.ContainsKey then
                                                                let ttype = M._compiledAssemblies.[M._compiledAssemblyNames.[cls]].GetType(cls)

                                                                result.AsManagedObject(ttype)
                                                            else
                                                                (cls |> obje_func(result, name)) :> obj
                                                            
                                                        with
                                                        | e when 
                                                            e :? Python.Runtime.PythonException && 
                                                            (e.ToString().Contains("RuntimeError") |> not) &&
                                                            (e.ToString().Contains("NameError") |> not) -> 

                                                                if "required positional argument" |> e.Message.Contains |> not then
                                                                    e.Message |> sbuilder.AppendLine |> ignore

                                                                e.StackTrace :> obj
                                                        | e -> 
                                                            "Error: " + e.ToString() |> Console.WriteLine
                                                            
                                                            if "required positional argument" |> e.Message.Contains |> not then
                                                                e.Message |> sbuilder.AppendLine |> ignore
                                                            e.StackTrace :> obj

                                                if 
                                                    func |> PyList.IsListType 
                                                    && (cls |> M._systemAssemblies.ContainsKey |> not)
                                                    && (cls |> M._compiledAssemblyNames.ContainsKey |> not)

                                                then
                                                    let resList = 
                                                        [
                                                            for r in func do 
                                                                let cls = r.GetPythonType().ToString().Replace("<class '","").Replace("'>","")
                                                                let obje = cls |> obje_func(r, name)
                                                                if obje |> isNull then
                                                                    yield r.ToString() :> obj
                                                                else
                                                                    yield obje
                                                        ]
                                                    resList :> obj

                                                elif cls |> M._systemAssemblies.ContainsKey then
                                                    let ttype = M._systemAssemblies.[cls].GetType(cls)
                                                    func.AsManagedObject(ttype)

                                                elif cls |> M._compiledAssemblyNames.ContainsKey then
                                                    let ttype = M._compiledAssemblies.[M._compiledAssemblyNames.[cls]].GetType(cls)

                                                    func.AsManagedObject(ttype)

                                                elif (func_str.StartsWith("<") && func_str.EndsWith(">") && cls <> "str") then
                                                    if func_str.Contains(" object at ") then
                                                        
                                                        try
                                                            // Need to transform PyObject to dict incase the object leaves the Gil
                                                            func 
                                                            |> Newtonsoft.Json.JsonConvert.SerializeObject
                                                            |> Newtonsoft.Json.JsonConvert.DeserializeObject
                                                            
                                                        with
                                                        | ex -> 
                                                            ex |> Console.WriteLine
                                                            if "required positional argument" |> ex.Message.Contains |> not then
                                                                ex.Message |> sbuilder.AppendLine |> ignore
                                                            inner_call
                                                        
                                                    else
                                                        inner_call
                                                
                                                elif inner_call |> isNull |> not then
                                                    inner_call
                                                else
                                                    // Need to transform PyObject to dict incase the object leaves the Gil
                                                    func 
                                                    |> Newtonsoft.Json.JsonConvert.SerializeObject
                                                    |> Newtonsoft.Json.JsonConvert.DeserializeObject

                                        if functionName |> isNull || functionName = "?" then
                                            let names = pyModule.Dir()
                                            for n in names do
                                                let n_str = n.ToString()
                                                let func = pyModule.GetAttr(n)
                                                if func |> isNull |> not then
                                                    let func_str = func.ToString()
                                                    if 
                                                        n_str |> isNull |> not &&
                                                        func_str |> isNull |> not &&
                                                        n_str.StartsWith("__") |> not && 
                                                        func_str.Contains("built-in") |> not && 
                                                        func_str.Contains("<module '") |> not &&
                                                        func_str.Contains("<class '") |> not 
                                                    then
                                                        let cls = func.GetPythonType().ToString().Replace("<class '","").Replace("'>","")

                                                        let funcModuleName =
                                                            try
                                                                func?__module__.ToString()
                                                            with
                                                            | _ -> ""
                                                        
                                                        if funcModuleName |> String.IsNullOrWhiteSpace || moduleName.Contains(funcModuleName) then
                                                            // List all function names "?"
                                                            let obje = 
                                                                if functionName = "?" then 
                                                                    let fname = pyName.ToLower()
                                                                    let doc = if documentation.ContainsKey(fname) then documentation.[fname] elif documentation.ContainsKey(fname + ".cs") then documentation.[fname + ".cs"] elif documentation.ContainsKey(fname + ".fs") then documentation.[fname + ".fs"] elif documentation.ContainsKey(fname + ".vb") then documentation.[fname + ".vb"] else (Seq.empty |> Map.ofSeq)
                                                                    let name = n.ToString()
                                                                    // (if doc.ContainsKey(name) then doc.[name] else {| Name = ""; Summary = ""; Remarks = ""; Returns = ""; Parameters = Seq.empty |}) :> obj
                                                                    (if doc.ContainsKey(name) then doc.[name] else null) :> obj
                                                                else 
                                                                    cls |> obje_func(func, n.ToString())
                                                            if obje |> isNull |> not then
                                                                let pair = (n.ToString(), obje, null)
                                                                pair |> resdb.Add

                                        else
                                            try
                                                let par = if parameters |> isNull then [||] else parameters |> Array.map(fun x -> PyString(x.ToString()) :> PyObject )
                                                let result = pyModule.InvokeMethod(functionName, par)
                                                
                                                let cls = result.GetPythonType().ToString().Replace("<class '","").Replace("'>","")
                                                
                                                let obje = cls |> obje_func(result, functionName)
                                                // obje |> Console.WriteLine

                                                if obje |> isNull |> not then
                                                    let pair = (functionName, obje, null)
                                                    pair |> resdb.Add
                                            with
                                            | e when 
                                                e :? Python.Runtime.PythonException && 
                                                (e.ToString().Contains("RuntimeError") |> not) &&
                                                (e.ToString().Contains("NameError") |> not) -> 

                                                // "--- ERR 2" |> Console.WriteLine
                                                // e |> Console.WriteLine

                                                // e.Message |> sbuilder.AppendLine |> ignore
                                                let pair = (functionName, null, e.Message :> obj)
                                                pair |> resdb.Add
                                            | ex ->
                                                let func = pyModule.GetAttr(functionName) 
                                                // let func_str = func.ToString()
                                                let cls = func.GetPythonType().ToString().Replace("<class '","").Replace("'>","")
                                                let obje = cls |> obje_func(func, functionName)

                                                if "required positional argument" |> ex.Message.Contains |> not then
                                                    // ex.Message |> sbuilder.AppendLine |> ignore
                                                    let pair = (functionName, null, ex.Message :> obj)
                                                    pair |> resdb.Add
                                                if obje |> isNull |> not then
                                                    let pair = (functionName, obje, null)
                                                    pair |> resdb.Add
                                
                            )
                        with
                        | :? NullReferenceException -> ()
                        | ex -> if "required positional argument" |> ex.Message.Contains |> not then ex.Message |> sbuilder.AppendLine |> ignore

                    let runJS (codes : (string * string) list) =
                        
                        try
                            let engine = Jint.Engine(fun cfg -> 
                                let assemblyList = 
                                    M._compiledAssemblies.Values |> Seq.toList
                                    |> List.append(
                                        [
                                            typeof<Newtonsoft.Json.JsonConverter>
                                            typeof<System.Data.SqlClient.SqlDataReader>

                                            typeof<QuantApp.Kernel.M>
                                            typeof<QuantApp.Engine.F>

                                            typeof<JVM.Runtime>

                                            typeof<Jint.Engine>
                                        ]
                                        |> List.map(fun f -> 
                                            Assembly.GetAssembly(f)
                                            ))

                                
                                cfg.AllowClr()
                                assemblyList
                                |> List.distinct
                                |> List.iter(fun ass -> cfg.AllowClr(ass) |> ignore)
                                )

                            engine.SetValue("log", new Action<obj>(Console.WriteLine))

                            engine.SetValue("jsWrapper", JsWrapper(engine))
                            codes 
                            |> List.iter(fun (name, code) ->
                                try
                                    code |> engine.Execute |> ignore
                                with
                                | :? Jint.Runtime.JavaScriptException as ex ->
                                    sbuilder.AppendLine("Error in: " + name)
                                    sbuilder.AppendLine(ex.Message) |> ignore
                                    sbuilder.AppendLine("       Starting at Line = " + ex.Location.Start.Line.ToString() + ", Column = " + ex.Location.Start.Column.ToString() + " ending at Line = " + ex.Location.End.Line.ToString() + ", Column = " + ex.Location.End.Column.ToString()) |> ignore
                                | :? Esprima.ParserException as ex ->
                                    sbuilder.AppendLine("Error in: " + name)
                                    sbuilder.AppendLine(ex.Message) |> ignore
                                )
                            
                            let alls_variables = engine.Global.GetOwnProperties()
                            if execute then
                                alls_variables 
                                |> Seq.iter(fun x -> 
                                    let name = x.Key
                                    try
                                        let prop = x.Value

                                        if prop |> isNull |> not then

                                            let isEnum = prop.Enumerable
                                            let valu = prop.Value
                                            
                                            if isEnum && name <> "jsWrapper" && valu |> isNull |> not && not(valu.ToString().StartsWith("[Namespace:")) then 

                                                if functionName |> String.IsNullOrWhiteSpace |> not && functionName = "?" |> not then
                                                    if functionName = name then
                                                        let t0 = DateTime.Now
                                                        // "Executing: " + name + " " + t0.ToString() |> Console.WriteLine
                                                        let valu_s = valu.ToString()
                                                        if valu_s.StartsWith("function()") then
                                                            let func = valu.ToObject() :?> Func<Jint.Native.JsValue,Jint.Native.JsValue[],Jint.Native.JsValue>
                                                            let res =
                                                                func.Invoke(
                                                                    Jint.Native.JsValue.Undefined,
                                                                    if parameters |> isNull then
                                                                        null
                                                                    else
                                                                        parameters 
                                                                        |> Array.map(fun x -> 
                                                                            if x.ToString().StartsWith("{\"") then
                                                                                let objr = x :> obj
                                                                                let res = Jint.Native.Json.JsonParser(engine).Parse(objr.ToString()) :> JsValue
                                                                            
                                                                                res
                                                                            
                                                                            elif x.ToString().StartsWith("{") then
                                                                                let str = Newtonsoft.Json.JsonConvert.SerializeObject(x) 
                                                                                let res = Jint.Native.Json.JsonParser(engine).Parse(str) :> JsValue
                                                                                
                                                                                res
                                                                            else
                                                                                Jint.Native.JsValue.FromObject(engine, x)
                                                                            )
                                                                        )

                                                            let res = 
                                                                if res = Jint.Native.JsValue.Undefined then
                                                                    null
                                                                else
                                                                    res.ToObject() :> obj
                                                            let pair = (name, res, null)

                                                            // "Executed: " + name + " " + (DateTime.Now - t0).ToString() |> Console.WriteLine
                                                            pair |> resdb.Add
                                                        else
                                                            let pair = (name, valu.ToObject() :> obj, null)
                                                            // "Executed: " + name + " " + (DateTime.Now - t0).ToString() |> Console.WriteLine
                                                            pair |> resdb.Add
                                                elif name.StartsWith("__") |> not then
                                                    let valu_s = valu.ToString()
                                                    
                                                    if valu_s.StartsWith("function()") then
                                                        let func = valu.ToObject() :?> Func<Jint.Native.JsValue,Jint.Native.JsValue[],Jint.Native.JsValue>
                                                        let func_obj = valu :?> Jint.Native.Function.ScriptFunctionInstance
                                                        let pars = func_obj.FormalParameters
                                                        
                                                        if functionName = "?" || pars.Length = 0 then
                                                            if functionName = "?" |> not then
                                                                let res =
                                                                    func.Invoke(
                                                                        Jint.Native.JsValue.Undefined,
                                                                        if parameters |> isNull then
                                                                            null
                                                                        else
                                                                            parameters 
                                                                            |> Array.map(fun x -> 
                                                                                if x.ToString().StartsWith("{\"") then
                                                                                    let objr = x :> obj
                                                                                    let res = Jint.Native.Json.JsonParser(engine).Parse(objr.ToString()) :> JsValue
                                                                                
                                                                                    res
                                                                                
                                                                                elif x.ToString().StartsWith("{") then
                                                                                    let str = Newtonsoft.Json.JsonConvert.SerializeObject(x) 
                                                                                    let res = Jint.Native.Json.JsonParser(engine).Parse(str) :> JsValue
                                                                                    
                                                                                    res
                                                                                else
                                                                                    Jint.Native.JsValue.FromObject(engine, x)
                                                                                )
                                                                            )
                                                                let res = 
                                                                    if res = Jint.Native.JsValue.Undefined then
                                                                        null
                                                                    else
                                                                        res.ToObject() :> obj
                                                                let pair = (name, res, null)
                                                                pair |> resdb.Add
                                                            else
                                                                let fname, _ = codes |> List.last
                                                                let fname = fname.ToLower()
                                                                let doc = if documentation.ContainsKey(fname) then documentation.[fname] elif documentation.ContainsKey(fname + ".js") then documentation.[fname + ".js"] else (Seq.empty |> Map.ofSeq)
                                                                // let docVal = (if doc.ContainsKey(name) then doc.[name] else {| Name = ""; Summary = ""; Remarks = ""; Returns = ""; Parameters = Seq.empty |}) :> obj
                                                                let docVal = (if doc.ContainsKey(name) then doc.[name] else null) :> obj

                                                                let pair = (name, docVal, null)
                                                                pair |> resdb.Add
                                                    else
                                                        let pair = (name, valu.ToObject() :> obj, null)
                                                        pair |> resdb.Add
                                    
                                    with
                                    | ex ->
                                        let pair = (name, null, ex.StackTrace :> obj)
                                        pair |> resdb.Add
                                
                            )
                        with
                        | :? Jint.Runtime.JavaScriptException as ex ->
                            ex.Message |> sbuilder.AppendLine |> ignore
                            "       Starting at Line = " + ex.Location.Start.Line.ToString() + ", Column = " + ex.Location.Start.Column.ToString() + " ending at Line = " + ex.Location.End.Line.ToString() + ", Column = " + ex.Location.End.Column.ToString() |> sbuilder.AppendLine |> ignore
                        | :? Esprima.ParserException as ex ->
                            ex.Message |> sbuilder.AppendLine |> ignore

                    let initJVM() =
                        if JVM.Runtime.Loaded |> not then

                            let jarsMntPath = DirectoryInfo("mnt/jars")
                            let jarsMnt =  jarsMntPath.GetFiles()
                            
                            jarsMnt |> Seq.map(fun jar -> jar.ToString()) |> Seq.iter(fun jar -> CompiledJVMBaseClasses.TryAdd(jar, jar) |> ignore)

                            let jarsPath = DirectoryInfo("jars")
                            let jars =  jarsPath.GetFiles()
                            
                            jars |> Seq.map(fun jar -> jar.ToString()) |> Seq.iter(fun jar -> CompiledJVMBaseClasses.TryAdd(jar, jar) |> ignore)

                            let jars = jars |> Seq.append(jarsMnt)

                            let path = jars |> Seq.map(fun jar -> jar.ToString()) |> Seq.fold(fun acc x -> acc + ":" + x) ""

                            if Runtime.InitJVM(classpath=path) <> 0 then

                                CompiledJVMBaseClasses.Values |> Seq.toArray |> Runtime.SetClassPath
                                
                                "JVM Engine not started: " + JVM.Runtime.Loaded.ToString() |> Console.WriteLine
                            else
                                "JVM Engine started" |> Console.WriteLine

                    let compileJava (codes : (string * string) list) =                    
                        initJVM()

                        let str = codes |> List.fold(fun acc (name, code) -> acc + code) ""
                        
                        let hash = str |> GetMd5Hash

                        if hash |> CompiledJVMClasses.ContainsKey |> not then
    
                            let compileJava (codes : (string * string) list) =
                                let path = Path.GetFileNameWithoutExtension(Path.GetTempFileName())
                                let path = Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), path)

                                let javaFiles =
                                    codes
                                    |> List.map(fun (name, code) ->
                                        let fn = Path.Combine(path, name)

                                        (FileInfo(fn)).Directory.Create()
                                        let javaFile = Path.ChangeExtension(fn, ".java")
                                        File.WriteAllText(javaFile, code)
                                        
                                        javaFile)

                                if codes |> List.isEmpty |> not && (snd codes.[0]).ToLower().Contains("package") then

                                    let errors, exitCode = 
                                        using (Py.GIL()) (fun _ -> 
                                            let file = javaFiles |> List.fold(fun acc file -> acc + ",'" + file.ToString() + "'" ) ""
                                            let cp = CompiledJVMBaseClasses.Values |> Seq.fold(fun acc path -> acc + ":" + path.ToString() ) ""
                                            let path = javaFiles |> List.head |> Path.GetDirectoryName

                                            let errorFile = Path.Combine(path, "errors.txt")
                                            let code = "import subprocess; subprocess.check_call(['javac', '-Xstdout', '" + errorFile + "', '-cp', '" + path + cp + "'" + file + "])"
                                            try
                                                code |> PythonEngine.Exec
                                                [|""|], 0
                                            with
                                            | _ -> [|File.ReadAllText(errorFile)|], -1
                                            )

                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(sbuilder.AppendLine >> ignore)
                                    #if MONO_LINUX || MONO_OSX
                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(Console.WriteLine)
                                    #endif
                                    if exitCode = 0 then
                                        (hash, path) |> CompiledJVMClasses.TryAdd |> ignore
                                        (hash, path) |> CompiledJVMBaseClasses.TryAdd |> ignore
                                        [|path|] |> Runtime.SetClassPath
                                else
                                    let errors, exitCode = 
                                        using (Py.GIL()) (fun _ -> 
                                            let file = javaFiles |> List.fold(fun acc file -> acc + ",'" + file.ToString() + "'" ) ""
                                            let cp = CompiledJVMBaseClasses.Values |> Seq.fold(fun acc path -> acc + ":" + path.ToString() ) ""
                                            let path = Path.GetDirectoryName(javaFiles |> List.head)

                                            let errorFile = Path.Combine(path, "errors.txt")
                                            let code = "import subprocess; subprocess.check_call(['javac', '-Xstdout', '" + errorFile + "', '-cp', '" + path + cp + "'" + file + "])"
                                            try
                                                code |> PythonEngine.Exec
                                                (hash, path) |> CompiledJVMClasses.TryAdd |> ignore
                                                [|""|], 0
                                            with
                                            | _ -> [|File.ReadAllText(errorFile)|], -1
                                            )

                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(sbuilder.AppendLine >> ignore)
                                    #if MONO_LINUX || MONO_OSX
                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(Console.WriteLine)
                                    #endif

                                    if exitCode = 0 then
                                        CompiledJVMClasses.TryAdd(hash, path) |> ignore

                            codes |> compileJava

                        
                        if execute && hash |> CompiledJVMClasses.ContainsKey then
                            try
                                let path = CompiledJVMClasses.[hash]
                                path 
                                |> Directory.GetFiles 
                                |> Array.filter(fun x -> x.EndsWith(".class") && x.Contains("$") |> not) 
                                |> Array.iter(fun x -> 
                                    try

                                        let className = x |> Path.GetFileNameWithoutExtension

                                        let jobj = Runtime.CreateInstancePath(className, path)

                                        jobj.Members.Keys
                                        |> Seq.iter(fun key -> 
                                            let func = jobj.Members.[key]
                                            let funcName = key.Substring(0, key.IndexOf("-"))
                                            let argSignature = key.Substring(key.IndexOf("-") + 1)
                                                
                                            try
                                                
                                                if (if functionName |> isNull || (functionName = "?" && (funcName.Contains("$") |> not) && (key = "equals-Ljava/lang/Object;" |> not)) then ("-" |> key.EndsWith || functionName = "?") else (functionName = funcName && (if parameters |> isNull || parameters |> Array.isEmpty then ("-" |> key.EndsWith) else ("-" |> key.EndsWith |> not)))) && key <> "toString-" && key <> "hashCode-" && key <> "getClass-" && key <> "clone-" && (func :? Runtime.wrapAction) |> not then

                                                    let parameters =
                                                        if parameters |> isNull then 
                                                            [||] 
                                                        else
                                                            let mutable idx = 0
                                                            parameters 
                                                            |> Array.mapi(fun i p -> 
                                                                if argSignature.Length = 0 || (argSignature.Length <= idx && argSignature.Length <> 0) then
                                                                    p :> obj
                                                                else
                                                                    let p = p.ToString()
                                                                    if argSignature.[idx] = 'L' then
                                                                        idx <- idx + argSignature.IndexOf(";") + 1
                                                                        p :> obj
                                                                    elif argSignature.[idx] = 'Z' then
                                                                        idx <- idx + 1
                                                                        System.Boolean.Parse(p) :> obj

                                                                    elif argSignature.[idx] = 'B' then
                                                                        idx <- idx + 1
                                                                        System.Byte.Parse(p) :> obj

                                                                    elif argSignature.[idx] = 'C' then
                                                                        idx <- idx + 1
                                                                        System.Char.Parse(p) :> obj

                                                                    elif argSignature.[idx] = 'S' then
                                                                        idx <- idx + 1
                                                                        System.Int16.Parse(p) :> obj

                                                                    elif argSignature.[idx] = 'I' then
                                                                        idx <- idx + 1
                                                                        System.Int32.Parse(p) :> obj

                                                                    elif argSignature.[idx] = 'J' then
                                                                        idx <- idx + 1
                                                                        System.Int64.Parse(p) :> obj

                                                                    elif argSignature.[idx] = 'F' then
                                                                        idx <- idx + 1
                                                                        System.Decimal.Parse(p) :> obj

                                                                    elif argSignature.[idx] = 'D' then
                                                                        idx <- idx + 1
                                                                        System.Double.Parse(p) :> obj

                                                                    else
                                                                        idx <- idx + 1
                                                                        p :> obj)
                                                    
                                                    try
                                                        if functionName = "?" |> not then
                                                            let value = jobj.InvokeMember(funcName, if parameters |> isNull then [||] else parameters)

                                                            if value |> isNull |> not then
                                                                let pair = (funcName, value, null)
                                                                pair |> resdb.Add
                                                        else

                                                            // "------ JAVA: " + className |> Console.WriteLine
                                                            let fname = className.ToLower()
                                                            let doc = if documentation.ContainsKey(fname) then documentation.[fname] elif documentation.ContainsKey(fname + ".java") then documentation.[fname + ".java"] else (Seq.empty |> Map.ofSeq)
                                                            let name = funcName
                                                            // let pair = (funcName, (if doc.ContainsKey(name) then doc.[name] else {| Name = ""; Summary = ""; Remarks = ""; Returns = ""; Parameters = Seq.empty |}) :> obj)
                                                            let pair = (funcName, (if doc.ContainsKey(name) then doc.[name] else null) :> obj, null)
                                                            pair |> resdb.Add
                                                    with
                                                    | ex -> 
                                                        let message = ex.Message.ToString()
                                                
                                                        if "Runtime Method not found" |> message.Contains |> not then
                                                            let pair = (funcName, null, message :> obj)
                                                            pair |> resdb.Add
                                            with
                                            | ex -> 
                                                let message = ex.Message.ToString()
                                                
                                                if "Runtime Method not found" |> message.Contains |> not then
                                                    let pair = (funcName, null, message :> obj)
                                                    pair |> resdb.Add
                                                    // message |> sbuilder.AppendLine |> ignore
                                                    // "--------------------------" |> sbuilder.AppendLine |> ignore
                                            )


                                        jobj.Properties.Keys
                                        |> Seq.iter(fun key -> 
                                            if (if functionName |> isNull then true else functionName = key) then
                                                try
                                                    let prop = jobj.TryGetMember(key)

                                                    let pair = (key, prop, null)
                                                    pair |> resdb.Add
                                                with
                                                | ex -> 
                                                    let message = ex.Message.ToString()
                                                    if "Runtime Method not found" |> message.Contains |> not then 
                                                        let pair = (key, null, message :> obj)
                                                        pair |> resdb.Add
                                            )
                                    with
                                    | _ -> ()
                                )
                            with
                            | ex -> 
                                ex.ToString() |> sbuilder.AppendLine |> ignore

                    let compileScala (codes : (string * string) list) =
                        initJVM()

                        let str = codes |> List.fold(fun acc (name, code) -> acc + code) ""
                        
                        let hash = str |> GetMd5Hash

                        if hash |> CompiledJVMClasses.ContainsKey |> not then
    
                            let compileScala (codes : (string * string) list) =
                                let path = Path.GetFileNameWithoutExtension(Path.GetTempFileName())
                                let path = Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), path)

                                let scalaFiles =
                                    codes
                                    |> List.map(fun (name, code) ->
                                        let fn = Path.Combine(path, name)

                                        (FileInfo(fn)).Directory.Create()
                                        let scalaFile = Path.ChangeExtension(fn, ".scala")
                                        File.WriteAllText(scalaFile, code)
                                        scalaFile)

                                if codes |> List.isEmpty |> not && (snd codes.[0]).ToLower().Contains("package") then
                                    
                                    let errors, exitCode, path = 
                                        using (Py.GIL()) (fun _ -> 
                                            let file = scalaFiles |> List.fold(fun acc file -> acc + ",'" + file.ToString() + "'" ) ""
                                            let cp = CompiledJVMBaseClasses.Values |> Seq.fold(fun acc path -> acc + ":" + path.ToString() ) ""
                                            let path = Path.GetDirectoryName(scalaFiles |> List.head)

                                            let errorFile = Path.Combine(path, "errors.txt")
                                            let code = "import subprocess; f = open('" + errorFile + "', 'w'); subprocess.check_call(['scalac', '-d', '" + path + "', '-cp', '" + path + cp + "'" + file + "], stdout=f, stderr=f)"
            
                                            try
                                                PythonEngine.Exec(code)
                                                [|""|], 0, path
                                            with
                                            | _ -> [|File.ReadAllText(errorFile)|], -1, ""
                                            )

                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(sbuilder.AppendLine >> ignore)
                                    #if MONO_LINUX || MONO_OSX
                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(Console.WriteLine)
                                    #endif
                                    if exitCode = 0 then
                                        CompiledJVMClasses.TryAdd(hash, path) |> ignore
                                        CompiledJVMBaseClasses.TryAdd(hash, path) |> ignore
                                        [|path|] |> Runtime.SetClassPath
                                else
                                    let errors, exitCode = 
                                        using (Py.GIL()) (fun _ -> 
                                            let file = scalaFiles |> List.fold(fun acc file -> acc + ",'" + file.ToString() + "'" ) ""
                                            let cp = CompiledJVMBaseClasses.Values |> Seq.fold(fun acc path -> acc + ":" + path.ToString() ) ""
                                            let path = Path.GetDirectoryName(scalaFiles |> List.head)

                                            let errorFile = Path.Combine(path, "errors.txt")
                                            let code = "import subprocess; f = open('" + errorFile + "', 'w'); subprocess.check_call(['scalac', '-d', '" + path + "', '-cp', '" + path + cp + "'" + file + "], stdout=f, stderr=f)"
                                            try
                                                PythonEngine.Exec(code)
                                                CompiledJVMClasses.TryAdd(hash, path) |> ignore
                                                [|""|], 0
                                            with
                                            | _ -> [|File.ReadAllText(errorFile)|], -1
                                            )

                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(sbuilder.AppendLine >> ignore)
                                    #if MONO_LINUX || MONO_OSX
                                    errors |> Array.filter(fun x -> x.ToString().ToLower().Contains("error")) |> Array.iter(Console.WriteLine)
                                    #endif

                                    if exitCode = 0 then
                                        CompiledJVMClasses.TryAdd(hash, path) |> ignore

                            codes |> compileScala

                        if execute && hash |> CompiledJVMClasses.ContainsKey then
                            try
                                let path = CompiledJVMClasses.[hash]
                                path 
                                |> Directory.GetFiles 
                                |> Array.filter(fun x -> x.EndsWith(".class") && x.Contains("$") |> not) 
                                |> Array.iter(fun x -> 
                                    try
                                        let className = x |> Path.GetFileNameWithoutExtension
                                        let jobj = Runtime.CreateInstancePath(className, path)
                                        if jobj |> isNull |> not then
                                            jobj.Members.Keys
                                            |> Seq.iter(fun key ->
                                                let func = jobj.Members.[key]
                                                let funcName = key.Substring(0, key.IndexOf("-"))
                                                let argSignature = key.Substring(key.IndexOf("-") + 1)
                                                
                                                try
                                                     
                                                    if (if functionName |> isNull || (functionName = "?" && (funcName.Contains("$") |> not) && (key = "equals-Ljava/lang/Object;" |> not)) then ("-" |> key.EndsWith || functionName = "?") else (functionName = funcName && (if parameters |> isNull || parameters |> Array.isEmpty then ("-" |> key.EndsWith) else ("-" |> key.EndsWith |> not)))) && key <> "toString-" && key <> "hashCode-" && key <> "getClass-" && key <> "clone-" && (func :? Runtime.wrapAction) |> not then
                                                        let parameters =
                                                            if parameters |> isNull then 
                                                                [||] 
                                                            else
                                                                let mutable idx = 0
                                                                parameters 
                                                                |> Array.mapi(fun i p -> 
                                                                    if argSignature.Length = 0 || (argSignature.Length <= idx && argSignature.Length <> 0) then
                                                                        p :> obj
                                                                    else
                                                                        let p = p.ToString()
                                                                        if argSignature.[idx] = 'L' then
                                                                            idx <- idx + argSignature.IndexOf(";") + 1
                                                                            p :> obj
                                                                        elif argSignature.[idx] = 'Z' then
                                                                            idx <- idx + 1
                                                                            System.Boolean.Parse(p) :> obj

                                                                        elif argSignature.[idx] = 'B' then
                                                                            idx <- idx + 1
                                                                            System.Byte.Parse(p) :> obj

                                                                        elif argSignature.[idx] = 'C' then
                                                                            idx <- idx + 1
                                                                            System.Char.Parse(p) :> obj

                                                                        elif argSignature.[idx] = 'S' then
                                                                            idx <- idx + 1
                                                                            System.Int16.Parse(p) :> obj

                                                                        elif argSignature.[idx] = 'I' then
                                                                            idx <- idx + 1
                                                                            System.Int32.Parse(p) :> obj

                                                                        elif argSignature.[idx] = 'J' then
                                                                            idx <- idx + 1
                                                                            System.Int64.Parse(p) :> obj

                                                                        elif argSignature.[idx] = 'F' then
                                                                            idx <- idx + 1
                                                                            System.Decimal.Parse(p) :> obj

                                                                        elif argSignature.[idx] = 'D' then
                                                                            idx <- idx + 1
                                                                            System.Double.Parse(p) :> obj

                                                                        else
                                                                            idx <- idx + 1
                                                                            p :> obj)
                                                        
                                                        try
                                                            if functionName = "?" |> not then
                                                                let value = jobj.InvokeMember(funcName, if parameters |> isNull then [||] else parameters)

                                                                if value |> isNull |> not then
                                                                    let pair = (funcName, value, null)
                                                                    pair |> resdb.Add
                                                            else
                                                                
                                                                let fname = className.ToLower()
                                                                let doc = if documentation.ContainsKey(fname) then documentation.[fname] elif documentation.ContainsKey(fname + ".scala") then documentation.[fname + ".scala"] else (Seq.empty |> Map.ofSeq)
                                                                let name = funcName
                                                                // let pair = (funcName, (if doc.ContainsKey(name) then doc.[name] else {| Name = ""; Summary = ""; Remarks = ""; Returns = ""; Parameters = Seq.empty |}) :> obj)
                                                                let pair = (funcName, (if doc.ContainsKey(name) then doc.[name] else null) :> obj, null)
                                                                
                                                                pair |> resdb.Add
                                                        with
                                                        | ex -> 
                                                            let message = ex.Message.ToString()
                                                    
                                                            if "Runtime Method not found" |> message.Contains |> not then
                                                                let pair = (funcName, null, message :> obj)
                                                                pair |> resdb.Add
                                                                
                                                with
                                                | ex -> 
                                                    let message = ex.Message.ToString()
                                                    
                                                    if "Runtime Method not found" |> message.Contains |> not then
                                                        let pair = (funcName, null, message :> obj)
                                                        pair |> resdb.Add
                                                        // message |> sbuilder.AppendLine |> ignore
                                                        // "--------------------------" |> sbuilder.AppendLine |> ignore
                                                )


                                            jobj.Properties.Keys
                                            |> Seq.iter(fun key -> 
                                                if (if functionName |> isNull then true else functionName = key) then
                                                    try
                                                        let prop = jobj.TryGetMember(key)
                                                        
                                                        let pair = (key, prop, null)
                                                        pair |> resdb.Add
                                                    with
                                                    | ex -> 
                                                        let message = ex.Message.ToString()
                                                        if "Runtime Method not found" |> message.Contains |> not then 
                                                            let pair = (key, null, message :> obj)
                                                            pair |> resdb.Add
                                                        
                                                )
                                    with
                                    | _ -> ()
                                )
                            with
                            | ex -> 
                                ex.ToString() |> sbuilder.AppendLine |> ignore

                    let compiled_net_code = codes |> List.filter(fun (name, x) -> (name.EndsWith(".cs") || name.EndsWith(".fs") || name.EndsWith(".vb")) && x.ToLower().Contains("namespace")) //Compiled
                    let scripted_net_code = codes |> List.filter(fun (name, x) -> (name.EndsWith(".cs") || name.EndsWith(".fs") || name.EndsWith(".vb")) && (x.ToLower().Contains("namespace") |> not)) //Script

                    let compiled_jvm_code = codes |> List.filter(fun (name, x) -> (name.EndsWith(".java") || name.EndsWith(".scala")) && x.ToLower().Contains("package")) //Compiled
                    let scripted_jvm_code = codes |> List.filter(fun (name, x) -> (name.EndsWith(".java") || name.EndsWith(".scala")) && (x.ToLower().Contains("package") |> not)) //Script

                    let python_code = codes |> List.filter(fun (name, x) -> name.EndsWith(".py")) //is Python
                    let js_code = codes |> List.filter(fun (name, x) -> name.EndsWith(".js")) //is Javascript

                    if compiled_net_code |> List.isEmpty |> not then 
                        let orderedProject = 
                            let mutable counter = 0
                            let mutable lastFlag = ""
                            compiled_net_code
                            |> List.mapi(fun i (name, code) -> 
                                let flag =
                                    match code with
                                    | x when x.StartsWith(csFlag) || name.EndsWith(".cs") -> csFlag
                                    | x when x.StartsWith(vbFlag) || name.EndsWith(".vb") -> vbFlag
                                    | _ -> fsFlag
                                if flag <> lastFlag then counter <- counter + 1
                                lastFlag <- flag
                                (name, code, counter)
                                )
                            |> List.groupBy(fun (name, code, counter) -> counter)
                            |> List.map(fun (g, files) -> (g, files |> List.map(fun (name, code, _) -> (name, code))))
                            

                        orderedProject
                        |> List.iter(fun (g, compiled_net_code) ->

                            let csFiles = compiled_net_code |> List.filter(fun (name, x) -> x.StartsWith(csFlag) || name.EndsWith(".cs"))
                            if csFiles |> List.isEmpty |> not then 
                                try csFiles |> compileCS with | _ -> 0 |> ignore

                            let vbFiles = compiled_net_code |> List.filter(fun (name, x) -> x.StartsWith(vbFlag) || name.EndsWith(".vb"))
                            if vbFiles |> List.isEmpty |> not then 
                                try vbFiles |> compileVB with | _ -> 0 |> ignore

                            let fsFiles = compiled_net_code |> List.filter(fun (name, x) -> x.StartsWith(fsFlag) || name.EndsWith(".fs"))
                            if fsFiles |> List.isEmpty |> not then 
                                try fsFiles |> compileFS with | _ -> 0 |> ignore
                            
                        )

                    if compiled_jvm_code |> List.isEmpty |> not then 
                        let orderedProject = 
                            let mutable counter = 0
                            let mutable lastFlag = ""
                            compiled_jvm_code
                            |> List.mapi(fun i (name, code) -> 
                                let flag =
                                    match code with
                                    | x when x.StartsWith(jvFlag) || name.EndsWith(".java") -> jvFlag
                                    | x when x.StartsWith(scFlag) || name.EndsWith(".scala") -> scFlag
                                    | _ -> jvFlag

                                if flag <> lastFlag then counter <- counter + 1
                                lastFlag <- flag
                                (name, code, counter)
                                )
                            |> List.groupBy(fun (name, code, counter) -> counter)
                            |> List.map(fun (g, files) -> (g, files |> List.map(fun (name, code, counter) -> (name, code))))
                            

                        orderedProject
                        |> List.iter(fun (g, compiled_jvm_code) ->

                            let jvFiles = compiled_jvm_code |> List.filter(fun (name, x) -> x.StartsWith(jvFlag) || name.EndsWith(".java"))
                            if jvFiles |> List.isEmpty |> not then 
                                try jvFiles |> compileJava with | _ -> 0 |> ignore

                            let scFiles = compiled_jvm_code |> List.filter(fun (name, x) -> x.StartsWith(scFlag) || name.EndsWith(".scala"))
                            if scFiles |> List.isEmpty |> not then 
                                try scFiles |> compileScala with | _ -> 0 |> ignore
                        )

                    if scripted_net_code |> List.isEmpty |> not then 
                        let csFiles = scripted_net_code |> List.filter(fun (name, x) -> x.StartsWith(csFlag) || name.EndsWith(".cs"))
                        if csFiles |> List.isEmpty |> not then csFiles |> compileCS

                        let vbFiles = scripted_net_code |> List.filter(fun (name, x) -> x.StartsWith(vbFlag) || name.EndsWith(".vb"))
                        if vbFiles |> List.isEmpty |> not then vbFiles |> compileVB
                        
                        let fsFiles = scripted_net_code |> List.filter(fun (name, x) -> x.StartsWith(fsFlag) || name.EndsWith(".fs"))
                        if fsFiles |> List.isEmpty |> not then fsFiles |> compileFS

                    if scripted_jvm_code |> List.isEmpty |> not then 
                        let jvFiles = scripted_jvm_code |> List.filter(fun (name, x) -> x.StartsWith(jvFlag) || name.EndsWith(".java"))
                        if jvFiles |> List.isEmpty |> not then jvFiles |> compileJava

                        let scFiles = scripted_jvm_code |> List.filter(fun (name, x) -> x.StartsWith(scFlag) || name.EndsWith(".scala"))
                        if scFiles |> List.isEmpty |> not then scFiles |> compileScala

                    if python_code |> List.isEmpty |> not then python_code |> runPython 

                    if js_code |> List.isEmpty |> not then js_code |> runJS 

            with
            | ex -> "QuantApp Compile ERROR: ---------------------------------------------------------------------" + Environment.NewLine + ex.ToString() |> Console.WriteLine

            codes |> Seq.toList 
            |> List.iter(fun (name, code) -> 
                let md5 = code |> GetMd5Hash
                if md5 |> CompiledBase.ContainsKey |> not then
                    // "Compiling: " + name |> Console.WriteLine 
                    
                    CompiledBase.TryAdd(md5, name) |> ignore)

            // { Result = (resdb |> Seq.toList); Exceptions = (expdb |> Seq.toList); Compilation = (sbuilder.ToString()) }
            { Result = (resdb |> Seq.toList); Compilation = (sbuilder.ToString()) }

        Utils.SetBuildCode(BuildCode(fun codes_all -> 
            ((codes_all, null, null) |> compileExecute(true, false)).Compilation))
        Utils.SetRegisterCode(RegisterCode(fun saveDisk execute codes_all -> 
            ((codes_all, null, null) |> compileExecute(saveDisk, execute)).Compilation))
        Utils.SetExecuteCode(ExecuteCode(fun codes_all -> 
            (codes_all, null, null) |> compileExecute(true, true)))
        Utils.SetExecuteCodeFunction(ExecuteCodeFunction(fun saveDisk codes name parameters -> 
            (codes, name, parameters) |> compileExecute(saveDisk, true)))
    let InitializeCode() = InitializeCodeTypes([||])
    
    let InstallNuGets (nugets : seq<NuGetPackage>) : unit =
        
        if nugets |> isNull |> not && nugets |> Seq.isEmpty |> not then
            nugets
            |> Seq.iter(fun nuget -> 
                try
                    let key = nuget.ID + nuget.Version
                    if key |> LoadedNuGets.ContainsKey |> not then
                        InstallNuGetAssembly nuget.ID nuget.Version
                        LoadedNuGets.TryAdd(key, key) |> ignore
                with
                | e -> e |> Console.WriteLine |> ignore
                )

    let InstallPips (pips : seq<PipPackage>) : unit =
        if pips |> isNull |> not && pips |> Seq.isEmpty |> not then
            pips
            |> Seq.iter(fun pip -> 
                try
                    let key = pip.ID
                    if key |> LoadedPips.ContainsKey |> not then
                        pip.ID |> InstallPip
                        LoadedPips.TryAdd(key, key) |> ignore
                with
                | e -> e.ToString() |> Console.WriteLine
                )

    let InstallJars (jars : seq<JarPackage>) : unit =
        
        if jars |> isNull |> not && jars |> Seq.isEmpty |> not then
            jars
            |> Seq.iter(fun jar -> 
                try
                    let key = jar.Url
                    if key |> LoadedJars.ContainsKey |> not then
                        jar.Url |> InstallJar
                        LoadedJars.TryAdd(key, key) |> ignore
                with
                | e -> e.ToString() |> Console.WriteLine
                )
    
    let ProcessPackageFile (pkg_file : string, registerBuild : bool) : PKG =
        let setListener (pkgID, file : string) = 
            let path = file |> Path.GetDirectoryName
            if path |> listeningPaths.ContainsKey |> not then
                let fileSystemWatcher = FileSystemWatcher()

                fileSystemWatcher.Path <- path
                fileSystemWatcher.NotifyFilter <- NotifyFilters.LastWrite
                fileSystemWatcher.EnableRaisingEvents <- true
                fileSystemWatcher.IncludeSubdirectories <- true

                fun (x : FileSystemEventArgs) ->
                    let file =  x.FullPath
                    let name = file |> Path.GetFileName
                    let code = file |> File.ReadAllText

                    let hash = code |> GetMd5Hash
                    
                    let lastBuiltHash = if file |> lastBuilt.ContainsKey then lastBuilt.[file] else ""

                    if lastBuiltHash <> hash then
                        lastBuilt.[file] <- hash

                        if registerBuild then
                            let t0 = DateTime.Now
                            "--------------------Build started: " + name + " @ " + t0.ToString() |> Console.WriteLine
                            let buildResult = Utils.RegisterCode (false, false) [name, code]

                            if buildResult |> String.IsNullOrEmpty then
                                "       building successful!!!" |> Console.WriteLine

                            let t1 = DateTime.Now
                            "--------------------Build done: " + name + " @ " + t0.ToString() + " ... " + (t1 - t0).ToString() |> Console.WriteLine
                            ""|>Console.WriteLine
                            ""|>Console.WriteLine
                        
                        "Saving changes to: " + name + " @ " + DateTime.Now.ToString() |> Console.WriteLine
                        
                        let work_books = pkgID + "--Queries" |> M.Base
                        let wb_res = work_books.[fun x -> M.V<string>(x, "Name") = name]
                        if wb_res.Count > 0 then
                            let item = wb_res.[0] :?> CodeData
                            work_books.Exchange(item, { item with Code = code })
                        work_books.Save()
                        

                |> fileSystemWatcher.Changed.Add

                listeningPaths.TryAdd(path, fileSystemWatcher) |> ignore


        let pkg_json = File.ReadAllText(pkg_file)
        let pkg_path = Path.GetDirectoryName(pkg_file)
        let pkg_type = Newtonsoft.Json.JsonConvert.DeserializeObject<QuantApp.Engine.PKG>(pkg_json)

        let pkg_id = if pkg_type.ID |> String.IsNullOrEmpty then System.Guid.NewGuid().ToString() else pkg_type.ID

        let parse_content (pkg : QuantApp.Engine.PKG) : QuantApp.Engine.PKG = 
            let base_content = 
                pkg.Base 
                |> Seq.map(fun entry -> 
                    let name, content = 
                        try
                            let name = if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content) else entry.Name
                            let content = File.ReadAllText(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content)

                            if name |> filePaths.ContainsKey then
                                filePaths.[name] <- pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content
                            else
                                filePaths.TryAdd(name, pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content) |> ignore

                            name, content
                        with
                        | _ -> 
                            try
                                (if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(entry.Content) else entry.Name), File.ReadAllText(entry.Content)
                            with
                            | _ -> entry.Name, entry.Content

                    { entry with Name = name; Content = content }
                )

            let agents_content = 
                pkg.Agents 
                |> Seq.map(fun entry -> 
                    let name, content, exe = 
                        let exe = if entry.Exe |> String.IsNullOrEmpty  then "pkg" else entry.Exe
                        try
                            let name = if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content) else entry.Name
                            let content = File.ReadAllText(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content)

                            if name |> filePaths.ContainsKey then
                                filePaths.[name] <- pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content
                            else
                                filePaths.TryAdd(name, pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content) |> ignore

                            name, content, exe
                        with
                        | _ -> 
                            try
                                (if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(entry.Content) else entry.Name), File.ReadAllText(entry.Content), exe
                            with
                            | _ -> entry.Name, entry.Content, exe
                    
                    { entry with Name = name; Content = content.Replace("$WID$", pkg_id); Exe = exe }
                )

            let queries_content = 
                pkg.Queries 
                |> Seq.map(fun entry -> 
                    let name, content = 
                        try
                            let name = if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content) else entry.Name
                            let content = File.ReadAllText(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content)

                            (pkg_id, pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content) |> setListener

                            if name |> filePaths.ContainsKey then
                                filePaths.[name] <- pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content
                            else
                                filePaths.TryAdd(name, pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content) |> ignore

                            name, content
                        with
                        | _ -> 
                            try
                                (if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(entry.Content) else entry.Name), File.ReadAllText(entry.Content)
                            with
                            | _ -> entry.Name, entry.Content

                    { entry with Name = name; Content = content.Replace("$WID$", pkg_id) }
                )

            let bins_content = 
                pkg.Bins 
                |> Seq.map(fun entry -> 
                    let name, content = 
                        try
                            let name = if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content) else entry.Name
                            let content = System.Convert.ToBase64String(File.ReadAllBytes(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content))
                            name, content
                        with
                        | _ -> 
                            try
                                (if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(entry.Content) else entry.Name), System.Convert.ToBase64String(File.ReadAllBytes(entry.Content))
                            with
                            | _ -> 
                                let files_m = pkg.ID + "--Bins" |> M.Base
                                let files_m_res = files_m.[fun x -> M.V<string>(x, "Name") = entry.Name]
                                let cont = 
                                    if files_m_res.Count > 0 then
                                        M.V<string>(files_m_res.[0],"Content")
                                    else
                                        entry.Content

                                entry.Name, cont
                    { entry with Name = name; Content = content }
                )
                |> Seq.toList |> List.toSeq

            let files_content = 
                pkg.Files 
                |> Seq.map(fun entry -> 
                    let name, content = 
                        try
                            let name = if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content) else entry.Name
                            let content = System.Convert.ToBase64String(File.ReadAllBytes(pkg_path + Path.DirectorySeparatorChar.ToString() + entry.Content))

                            name, content
                        with
                        | e ->
                            // e |> Console.WriteLine 
                            try
                                (if entry.Name |> String.IsNullOrEmpty then Path.GetFileName(entry.Content) else entry.Name), System.Convert.ToBase64String(File.ReadAllBytes(entry.Content))
                            with
                            | _ ->
                                let files_m = pkg.ID + "--Files" |> M.Base

                                let files_m_res = files_m.[fun x -> M.V<string>(x, "Name") = entry.Name]
                                let cont = 
                                    if files_m_res.Count > 0 then
                                        M.V<string>(files_m_res.[0],"Content")
                                    else
                                        entry.Content

                                entry.Name, cont
                    { entry with Name = name; Content = content }
                )
                |> Seq.toList |> List.toSeq

            let readme_content = File.ReadAllText(pkg_path + Path.DirectorySeparatorChar.ToString() + pkg.ReadMe)
            
            { 
                pkg with 
                    Base = base_content
                    Agents = agents_content
                    Queries = queries_content
                    Bins = bins_content
                    Files = files_content
                    ReadMe = readme_content
                    NuGets = if pkg.NuGets |> isNull then [] |> List.toSeq else pkg.NuGets 
                    Pips = if pkg.Pips |> isNull then [] |> List.toSeq else pkg.Pips 
            }

        pkg_type |> parse_content        

    let ProcessPackageDictionary (pkg_dict : Collections.Generic.Dictionary<string, string>) : PKG =
        let pkg_json = pkg_dict.["package.json"]
        let pkg_type = Newtonsoft.Json.JsonConvert.DeserializeObject<QuantApp.Engine.PKG>(pkg_json)

        let pkg_id = if pkg_type.ID |> String.IsNullOrEmpty then System.Guid.NewGuid().ToString() else pkg_type.ID

        let parse_content (pkg : QuantApp.Engine.PKG) : QuantApp.Engine.PKG = 
            let base_content = 
                pkg.Base 
                |> Seq.map(fun entry -> 
                    let content = pkg_dict.[(if "Base/" |> entry.Name.StartsWith then "" else "Base/") + entry.Name]
                    { entry with Content = content }
                )

            let agents_content = 
                pkg.Agents 
                |> Seq.map(fun entry -> 
                    let content = pkg_dict.["Agents/" + entry.Name]
                        
                    { entry with Content = content }
                )

            let queries_content = 
                pkg.Queries 
                |> Seq.map(fun entry -> 
                    let content = pkg_dict.["Queries/" + entry.Name]
                        
                    { entry with Content = content }
                )

            let bins_content = 
                pkg.Bins 
                |> Seq.map(fun entry -> 
                    let content = pkg_dict.["Bins/" + entry.Name]
                    // content |> Console.WriteLine
                        
                    { entry with Content = content }
                )

            let files_content = 
                pkg.Files 
                |> Seq.map(fun entry -> 
                    let content = pkg_dict.["Files/" + entry.Name]
                        
                    { entry with Content = content }
                )
                
            { 
                pkg with 
                    Base = base_content
                    Agents = agents_content
                    Queries = queries_content
                    Bins = bins_content
                    Files = files_content 
                    ReadMe = pkg_dict.["README.md"]
            }

        let pkg_content = pkg_type |> parse_content
        pkg_content
    
    let BuildRegisterPackage (pkg_content : PKG) =

        CompiledPackages.Keys |> Seq.iter(fun key -> "Compiled Packages: " + CompiledPackages.[key] + " " + key |> Console.WriteLine)

        let not_compiled_hashes = 
            pkg_content.Base 
            |> Seq.toList 
            |> List.map(fun entry -> entry.Name, entry.Content) 
            |> List.map(fun (_, content) -> content |> GetMd5Hash)
            |> List.filter(CompiledBase.ContainsKey >> not)
            |> List.isEmpty
            |> not

        if 
            not_compiled_hashes

            && CompiledPackages.ContainsKey(pkg_content.ID)
        then
            "New base library compiled..." |> Console.WriteLine
            #if MONO_LINUX || MONO_OSX
            "Trying to restart runtime..." |> Console.WriteLine
            raise (System.SystemException("New base library compiled"))
            #endif

        pkg_content.NuGets |> InstallNuGets
        pkg_content.Pips |> InstallPips
        pkg_content.Jars |> InstallJars

        let buildBase =
            pkg_content.Base |> Seq.toList |> List.map(fun entry -> entry.Name, entry.Content)
            |> Utils.RegisterCode(false, false)

        let buildAgents =
            if buildBase |> String.IsNullOrEmpty then
                pkg_content.Agents |> Seq.toList |> List.map(fun entry -> entry.Name, entry.Content.Replace("$WID$", pkg_content.ID))
                |> Utils.RegisterCode(false, false)
            else
                "Agents not compiled"

        let buildQueries =
            if buildAgents |> String.IsNullOrEmpty then
                pkg_content.Queries |> Seq.toList |> List.map(fun entry -> entry.Name, entry.Content)
                |> Utils.BuildCode
            else
                "Queries not compiled"

        if pkg_content.ID |> CompiledPackages.ContainsKey |> not then
            CompiledPackages.TryAdd(pkg_content.ID, pkg_content.ID) |> ignore
            CompiledBase.Clear()
            pkg_content.Base |> Seq.toList 
            |> List.iter(fun entry -> 
                let md5 = entry.Content |> GetMd5Hash
                if md5 |> CompiledBase.ContainsKey |> not then
                    CompiledBase.TryAdd(md5, entry.Name) |> ignore)

        [buildBase; buildAgents; buildQueries]
        |> List.toSeq
        |> Seq.filter(String.IsNullOrEmpty >> not)
        |> Seq.collect(fun (build : string) -> build.Split([|Environment.NewLine|], StringSplitOptions.None) |> Array.toSeq)
        |> Seq.fold(fun (acc : StringBuilder) line -> acc.AppendLine(line)) (StringBuilder())
        |> (fun x -> x.ToString())

    let BuildCompileOnlyPackage (pkg_content : PKG) =

        if 
            pkg_content.Base 
            |> Seq.toList 
            |> List.map(
                fun entry -> entry.Name, entry.Content
                >>
                fun (name, content) -> content |> GetMd5Hash)
            |> List.filter(M._compiledAssemblies.ContainsKey)
            |> List.isEmpty
        then
            "New base library compiled..." |> Console.WriteLine

        pkg_content.NuGets |> InstallNuGets
        pkg_content.Pips |> InstallPips
        pkg_content.Jars |> InstallJars

        let buildBase =
            pkg_content.Base |> Seq.toList |> List.map(fun entry -> entry.Name, entry.Content)
            |> Utils.BuildCode

        let buildAgents =
            if buildBase |> String.IsNullOrEmpty then
                pkg_content.Agents |> Seq.toList |> List.map(fun entry -> entry.Name, entry.Content.Replace("$WID$", pkg_content.ID))
                |> Utils.BuildCode
            else
                "Agents not compiled"

        let buildQueries =
            if buildAgents |> String.IsNullOrEmpty then
                pkg_content.Queries |> Seq.toList |> List.map(fun entry -> entry.Name, entry.Content)
                |> Utils.BuildCode
            else
                "Queries not compiled"

        [buildBase; buildAgents; buildQueries]
        |> List.toSeq
        |> Seq.filter(String.IsNullOrEmpty >> not)
        |> Seq.collect(fun (build : string) -> build.Split([|Environment.NewLine|], StringSplitOptions.None) |> Array.toSeq)
        |> Seq.fold(fun (acc : StringBuilder) line -> acc.AppendLine(line)) (StringBuilder())
        |> (fun x -> x.ToString())

    let ProcessPackageJSON (pkg_content : PKG) =
        
        let build = pkg_content |> BuildRegisterPackage
        if build |> String.IsNullOrEmpty then
            let ws =
                let pkg_id = pkg_content.ID

                let filesCache = pkg_content.Files |> Seq.toList
                let binsCache = pkg_content.Bins |> Seq.toList

                let files_m = pkg_id + "--Files" |> M.Base
                files_m.[fun _ -> true] 
                |> Seq.iter(
                    fun fileEntry ->
                        let name = M.V<string>(fileEntry, "Name") 
                        
                        filesCache
                        |> List.iter(
                            fun pkgFile ->
                                if pkgFile.Name = name && pkgFile.Content <> "__content__in__m__" then
                                    fileEntry |> files_m.Remove
                        )
                        
                        // file |> files_m.Remove
                    )
                files_m.Save()

                let bins_m = pkg_id + "--Bins" |> M.Base
                bins_m.[fun _ -> true] 
                |> Seq.iter(
                    fun fileEntry ->
                        let name = M.V<string>(fileEntry, "Name") 
                        
                        binsCache 
                        |> List.iter(
                            fun pkgFile ->
                                if pkgFile.Name = name && pkgFile.Content <> "__content__in__m__" then
                                    fileEntry |> bins_m.Remove
                        )
                        
                        // file |> files_m.Remove
                    )
                // |> Seq.iter(bins_m.Remove)
                bins_m.Save()

                let ws = 
                    {
                        ID = pkg_id
                        Name = pkg_content.Name
                        Strategies = []
                        Agents = 
                            pkg_content.Agents
                            |> Seq.toList
                            |> List.map(fun entry -> 
                                let fpkg, code = 
                                    Utils.CreatePKG(
                                    [
                                        (entry.Name, entry.Content.Replace("$WID$", pkg_content.ID))
                                    ],
                                    entry.Exe, [||])
                                let fpkg = { fpkg with ID = fpkg.ID.Replace("$WID$", pkg_id); WorkflowID = fpkg.WorkflowID.Replace("$WID$", pkg_id) }
                                let f, result = F.CreatePKG(fpkg, code)
                                async { { Function = "Main"; Data = "Initial Execution" } |> Newtonsoft.Json.JsonConvert.SerializeObject |> f.Body |> ignore } |> Async.Start
                                f, result
                                )
                            |> List.map(fun (f, _)  -> f.ID)

                        Permissions = pkg_content.Permissions |> Seq.toList

                        Code = pkg_content.Base |> Seq.toList |> List.map(fun entry -> entry.Name, entry.Content)
                        NuGets = pkg_content.NuGets |> Seq.toList
                        Pips = pkg_content.Pips |> Seq.toList
                        Jars = pkg_content.Jars |> Seq.toList
                        // Bins = pkg_content.Bins |> Seq.toList
                        // Files = pkg_content.Files |> Seq.toList
                        Bins = 
                            binsCache
                            |> List.map(fun filePkg ->
                                if filePkg.Content = "__content__in__m__" |> not then
                                    let bins_m_res = bins_m.[fun x -> M.V<string>(x, "Name") = filePkg.Name]
                                    if bins_m_res.Count > 0 then
                                        let item = bins_m_res.[0] :?> FilePackage
                                        
                                        bins_m.Exchange(
                                            item, 
                                            filePkg)
                                    else
                                        bins_m.Add(
                                            filePkg) |> ignore

                                {    
                                    Name = filePkg.Name
                                    Content = "__content__in__m__"
                                } : FilePackage)
                            
                        Files = 
                            filesCache
                            |> List.map(fun filePkg ->
                                if filePkg.Content = "__content__in__m__" |> not then
                                    let files_m_res = files_m.[fun x -> M.V<string>(x, "Name") = filePkg.Name]
                                    if files_m_res.Count > 0 then
                                        let item = files_m_res.[0] :?> FilePackage
                                        
                                        files_m.Exchange(
                                            item, 
                                            filePkg)
                                    else
                                        files_m.Add(
                                            filePkg) |> ignore

                                {    
                                    Name = filePkg.Name
                                    Content = "__content__in__m__"
                                } : FilePackage)
                            
                        ReadMe = pkg_content.ReadMe
                        Publisher = if pkg_content.Publisher |> isNull then QuantApp.Kernel.User.ContextUser.Email else pkg_content.Publisher
                        PublishTimestamp = if pkg_content.PublishTimestamp.Year <= (DateTime.Now.Year - 10) then DateTime.Now else pkg_content.PublishTimestamp
                        AutoDeploy = pkg_content.AutoDeploy
                        Container = pkg_content.Container
                    }

                files_m.Save()
                bins_m.Save()

                let work_books = pkg_id + "--Queries" |> M.Base
                work_books.[fun _ -> true] |> Seq.iter(work_books.Remove)
                work_books.Save()
                
                pkg_content.Queries
                |> Seq.toList
                |> List.iter(fun entry ->
                    let wb_res = work_books.[fun x -> M.V<string>(x, "ID") = entry.ID]
                    if wb_res.Count > 0 then
                        let item = wb_res.[0] :?> CodeData
                        
                        work_books.Exchange(
                            item, 
                            {    
                                Name = entry.Name
                                ID = if entry.ID |> String.IsNullOrEmpty then System.Guid.NewGuid().ToString() else entry.ID
                                Code = entry.Content
                                WorkflowID = pkg_id
                            })
                    else
                        work_books.Add(
                            {    
                                Name = entry.Name
                                ID = if entry.ID |> String.IsNullOrWhiteSpace then System.Guid.NewGuid().ToString() else entry.ID
                                Code = entry.Content
                                WorkflowID = pkg_id
                            }) |> ignore
                )
                work_books.Save()
                ws

            let wsp = ws.ID |> M.Base
            wsp.[fun _ -> true] |> Seq.iter(wsp.Remove)
            ws |> wsp.Add |> ignore

            wsp.Save()

        else
            "Build result: " |> Console.WriteLine
            build |> Console.WriteLine

        build

    let ProcessPackageWorkflow (wsp : Workflow) : PKG =
        
        let pkg_id = wsp.ID

        let queries =
            let work_books = M.Base(pkg_id + "--Queries")
            if work_books |> isNull |> not then
                work_books.[fun _ -> true] 
                |> Seq.map(
                        M.C<CodeData> 
                        >>
                        fun entry ->
                        {
                            Name = entry.Name
                            Content = entry.Code
                            ID = entry.ID
                        })
            else
                null

        let agents = 
            wsp.Agents
            |> List.map(fun id -> 
                let m = id+ "-F-MetaData" |> M.Base

                let res = m.[fun x-> M.V<string>(x,"ID") = id]
                let pkg = res.[0] :?> FMeta

                let name, code = pkg.Code |> Seq.head
                
                {
                    Name = name
                    Content = code
                    Exe = "pkg"
                })

        let files_m = pkg_id + "--Files" |> M.Base
        let bins_m = pkg_id + "--Bins" |> M.Base
        {
            ID = pkg_id
            Name = wsp.Name
            Base = wsp.Code |> List.toSeq |> Seq.map(fun (name, code) -> { Name = name; Content = code })
            Agents = agents
            Queries = queries
            Permissions = wsp.Permissions |> Seq.toList
            NuGets = wsp.NuGets |> Seq.toList
            Pips = wsp.Pips |> Seq.toList
            Jars = wsp.Jars |> Seq.toList
            // Bins = wsp.Bins |> Seq.toList
            // Files = wsp.Files |> Seq.toList
            Bins =
                wsp.Bins
                |> Seq.map(fun filePkg ->
                    
                    let bins_m_res = bins_m.[fun x -> M.V<string>(x, "Name") = filePkg.Name]
                    if bins_m_res.Count > 0 then
                        bins_m_res.[0] :?> FilePackage
                    else
                        filePkg)
            Files =
                wsp.Files
                |> Seq.map(fun filePkg ->
                    
                    let files_m_res = files_m.[fun x -> M.V<string>(x, "Name") = filePkg.Name]
                    if files_m_res.Count > 0 then
                        files_m_res.[0] :?> FilePackage
                    else
                        filePkg)
            ReadMe = wsp.ReadMe
            Publisher = if wsp.Publisher |> isNull then QuantApp.Kernel.User.ContextUser.Email else wsp.Publisher
            PublishTimestamp = if wsp.PublishTimestamp.Year <= (DateTime.Now.Year - 10) then DateTime.Now else wsp.PublishTimestamp
            AutoDeploy = wsp.AutoDeploy
            Container = wsp.Container
        }
    
    let ProcessPackageToZIP (pkg : PKG) : byte[] =
        let memoryStream = MemoryStream()
        
        using (ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            (fun archive -> 
                pkg.Base
                |> Seq.iter(fun entry -> 
                    let file = archive.CreateEntry((if "Base/" |> entry.Name.StartsWith then "" else "Base/") + entry.Name, CompressionLevel.Optimal)

                    let entryStream = file.Open()
                    let streamWriter = StreamWriter(entryStream)
                    
                    streamWriter.Write(entry.Content)
                    streamWriter.Close()
                    )

                pkg.Agents
                |> Seq.iter(fun entry -> 
                    let file = archive.CreateEntry("Agents/" + entry.Name, CompressionLevel.Optimal)

                    let entryStream = file.Open()
                    let streamWriter = StreamWriter(entryStream)
                    
                    streamWriter.Write(entry.Content)
                    streamWriter.Close()
                    )

                pkg.Queries
                |> Seq.iter(fun entry -> 
                    let file = archive.CreateEntry("Queries/" + entry.Name, CompressionLevel.Optimal)

                    let entryStream = file.Open()
                    let streamWriter = StreamWriter(entryStream)
                    
                    streamWriter.Write(entry.Content)
                    streamWriter.Close()
                    )

                let bins_m = pkg.ID + "--Bins" |> M.Base
                pkg.Bins
                |> Seq.iter(fun entry -> 
                    let file = archive.CreateEntry("Bins/" + entry.Name, CompressionLevel.Optimal)

                    let entryStream = file.Open()
                    let streamWriter = BinaryWriter(entryStream)
                    
                    let bins_m_res = bins_m.[fun x -> M.V<string>(x, "Name") = entry.Name]
                    if bins_m_res.Count > 0 then
                        // let item = bins_m_res.[0] :?> FilePackage
                        let item = bins_m_res.[0]
                        try
                            // streamWriter.Write(System.Convert.FromBase64String(entry.Content))
                            streamWriter.Write(System.Convert.FromBase64String(M.V<string>(item, "Content")))
                        with | _ -> 0 |> ignore
                        streamWriter.Close()
                    )

                let files_m = pkg.ID + "--Files" |> M.Base
                pkg.Files
                |> Seq.iter(fun entry -> 
                    let file = archive.CreateEntry("Files/" + entry.Name, CompressionLevel.Optimal)

                    let entryStream = file.Open()
                    let streamWriter = BinaryWriter(entryStream)

                    let files_m_res = files_m.[fun x -> M.V<string>(x, "Name") = entry.Name]
                    if files_m_res.Count > 0 then
                        // let item = files_m_res.[0] :?> FilePackage
                        let item = files_m_res.[0]
                        try
                            // streamWriter.Write(System.Convert.FromBase64String(item.Content))
                            streamWriter.Write(System.Convert.FromBase64String(M.V<string>(item, "Content")))
                        with | _ -> 0 |> ignore
                        streamWriter.Close()
                    )

                let file = archive.CreateEntry("README.md", CompressionLevel.Optimal)

                let entryStream = file.Open()
                let streamWriter = StreamWriter(entryStream)
                streamWriter.Write(pkg.ReadMe)
                streamWriter.Close()


                let file = archive.CreateEntry("package.json", CompressionLevel.Optimal)

                let entryStream = file.Open()
                let streamWriter = StreamWriter(entryStream)

                
                let pkg_paths = 
                    {
                        pkg with
                            Base =
                                pkg.Base
                                |> Seq.map(fun entry -> { entry with Content =(if "Base/" |> entry.Name.StartsWith then "" else "Base/") + entry.Name });
                            Agents =
                                pkg.Agents
                                |> Seq.map(fun entry -> { entry with Content = "Agents/" + entry.Name });
                            Queries =
                                pkg.Queries
                                |> Seq.map(fun entry -> { entry with Content = "Queries/" + entry.Name })

                            Bins =
                                pkg.Bins
                                |> Seq.map(fun entry -> { entry with Content = "Bins/" + entry.Name })

                            Files =
                                pkg.Files
                                |> Seq.map(fun entry -> { entry with Content = "Files/" + entry.Name })

                            ReadMe = "README.md"
                    }
        
                let pkg_paths = Newtonsoft.Json.JsonConvert.SerializeObject(pkg_paths, Newtonsoft.Json.Formatting.Indented)
                streamWriter.Write(pkg_paths)
                streamWriter.Close()
            )

        memoryStream.Seek(int64(0), SeekOrigin.Begin)
        memoryStream.ToArray()
    
    let ProcessPackageFromZip (data : byte[]) : PKG =
        let archive = ZipArchive(MemoryStream(data))
        let files = Collections.Generic.Dictionary<string, string>()
        archive.Entries
        |> Seq.iter(fun entry ->
            let entryStream = entry.Open()
            
            let content = 
                if entry.FullName.Contains("Bins/") || entry.FullName.Contains("Files/") then
                    
                    let memoryStream = MemoryStream()
                    entryStream.CopyTo(memoryStream)
                    
                    System.Convert.ToBase64String(memoryStream.ToArray())
                else
                    let streamReader = StreamReader(entryStream)
                    streamReader.ReadToEnd()
            files.Add(entry.FullName, content)
            )

        files |> ProcessPackageDictionary

    let ProcessPackageFromGit (data : byte[]) : PKG =
        let archive = ZipArchive(MemoryStream(data))
        let files = Collections.Generic.Dictionary<string, string>()
        archive.Entries
        |> Seq.iter(fun entry ->
            let entryStream = entry.Open()

            let content = 
                if entry.FullName.Contains("Bins/") || entry.FullName.Contains("Files/") then
                    
                    let memoryStream = MemoryStream()
                    entryStream.CopyTo(memoryStream)
                    
                    System.Convert.ToBase64String(memoryStream.ToArray())

                else
                    let streamReader = StreamReader(entryStream)
                    streamReader.ReadToEnd()

            files.Add(entry.FullName.Substring(entry.FullName.IndexOf("/") + 1), content)
            )

        files |> ProcessPackageDictionary

    let UpdatePackageFile (pkg_file : string, nuget: NuGetPackage, pip: PipPackage, jar: JarPackage) : string =
        let pkgJson = File.ReadAllText(pkg_file)
        let pkgPath = Path.GetDirectoryName(pkg_file)
        let pkgType = Newtonsoft.Json.JsonConvert.DeserializeObject<QuantApp.Engine.PKG>(pkgJson)

        let pkgId = if pkgType.ID |> String.IsNullOrEmpty then System.Guid.NewGuid().ToString() else pkgType.ID

        let parse_content (pkg : QuantApp.Engine.PKG) : QuantApp.Engine.PKG = 

            let baseContent : seq<PKG_Base> =
                if pkgPath + "/Base" |> Directory.Exists then

                    let baseNames = pkg.Base |> Seq.map(fun entry -> entry.Content)
                    
                    (pkgPath + "/Base", "*", SearchOption.AllDirectories)
                    |> Directory.GetFiles
                    |> Seq.map(
                        fun entry -> entry.Replace(Path.DirectorySeparatorChar.ToString(), "/")
                        >>
                        fun entry -> entry.Substring(entry.IndexOf("/Base") + 1))
                    |> Seq.filter(fun entry -> entry.EndsWith(".pyc") |> not)    
                    |> Seq.filter(fun entry -> entry.EndsWith(".DS_Store") |> not)    
                    |> Seq.filter(fun entry -> baseNames |> Seq.contains(entry) |> not)
                    |> Seq.map(fun entry -> 
                        let id = if ".py" |> entry.EndsWith then entry else entry.Substring(entry.IndexOf("/Base") + "/Base".Length + 1)
                        {
                            Name = id
                            Content = entry
                        }
                    )
                else
                    Seq.empty

            let queriesContent : seq<PKG_Query> =
                if pkgPath + "/Queries" |> Directory.Exists then

                    let queryNames = pkg.Queries |> Seq.map(fun entry -> entry.Content)
                    
                    (pkgPath + "/Queries", "*", SearchOption.AllDirectories)
                    |> Directory.GetFiles
                    |> Seq.map(
                        fun entry -> entry.Replace(Path.DirectorySeparatorChar.ToString(), "/")
                        >>
                        fun entry -> entry.Substring(entry.IndexOf("/Queries") + 1))
                    |> Seq.filter(fun entry -> entry.EndsWith(".DS_Store") |> not)  
                    |> Seq.filter(fun entry -> entry.EndsWith(".pyc") |> not)      
                    |> Seq.filter(fun entry -> queryNames |> Seq.contains(entry) |> not)
                    |> Seq.map(fun entry ->
                        let id = entry.Substring(entry.IndexOf("/Queries") + "/Queries".Length + 1)
                        {
                            ID = id
                            Name = id
                            Content = entry
                        }
                    )
                else
                    Seq.empty

            let agentContent : seq<PKG_Agent> =
                if pkgPath + "/Agents" |> Directory.Exists then

                    let agentNames = pkg.Agents |> Seq.map(fun entry -> entry.Content)
                    
                    (pkgPath + "/Agents", "*", SearchOption.AllDirectories)
                    |> Directory.GetFiles
                    |> Seq.map(
                        fun entry -> entry.Replace(Path.DirectorySeparatorChar.ToString(), "/")
                        >>
                        fun entry -> entry.Substring(entry.IndexOf("/Agents") + 1))
                    |> Seq.filter(fun entry -> agentNames |> Seq.contains(entry) |> not)
                    |> Seq.filter(fun entry -> entry.EndsWith(".pyc") |> not)    
                    |> Seq.filter(fun entry -> entry.EndsWith(".DS_Store") |> not)    
                    |> Seq.map(fun entry ->
                        let id = entry.Substring(entry.IndexOf("/Agents") + "/Agents".Length + 1)
                        {
                            Exe = "pkg"
                            Name = id
                            Content = entry
                        }
                    )
                else
                    Seq.empty
            
            let binsContent = 
                if pkgPath + "/Bins" |> Directory.Exists then
                    
                    (pkgPath + "/Bins", "*", SearchOption.AllDirectories)
                    |> Directory.GetFiles
                    |> Seq.filter(fun entry -> entry.EndsWith(".DS_Store") |> not)    
                    |> Seq.map(
                        fun entry -> entry.Replace(Path.DirectorySeparatorChar.ToString(), "/")
                        >>
                        fun entry -> 
                            { Name = entry.Substring(entry.IndexOf("/Bins") + "/Bins".Length + 1); Content = entry.Substring(entry.IndexOf("/Bins") + 1)}
                    )
                    
                else
                    Seq.empty

            let filesContent = 
                if pkgPath + "/Files" |> Directory.Exists then
                    
                    (pkgPath + "/Files", "*", SearchOption.AllDirectories)
                    |> Directory.GetFiles
                    |> Seq.filter(fun entry -> entry.EndsWith(".DS_Store") |> not)    
                    |> Seq.map(
                        fun entry -> entry.Replace(Path.DirectorySeparatorChar.ToString(), "/")
                        >>
                        fun entry -> 
                            { Name = entry.Substring(entry.IndexOf("/Files") + "/Files".Length + 1); Content = entry.Substring(entry.IndexOf("/Files") + 1)}
                    )
                else
                    Seq.empty

            { 
                pkg with 
                    ID = pkgId
                    Base = baseContent |> Seq.append(pkg.Base)
                    Queries = queriesContent |> Seq.append(pkg.Queries)
                    Agents = agentContent |> Seq.append(pkg.Agents)
                    Bins = binsContent
                    Files = filesContent
                    Pips = if pip.ID |> isNull then pkg.Pips else (pkg.Pips |> Seq.append([pip]))
                    NuGets = if nuget.ID |> isNull then pkg.NuGets else (pkg.NuGets |> Seq.append([nuget]))
                    Jars = if jar.Url |> isNull then pkg.Jars else (pkg.Jars |> Seq.append([jar]))
            }

        let pkgContent = pkgType |> parse_content        

        File.WriteAllText(pkg_file, Newtonsoft.Json.JsonConvert.SerializeObject(pkgContent, Newtonsoft.Json.Formatting.Indented))
        pkg_file

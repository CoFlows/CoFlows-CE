/// <info version="1.0.100">
///     <title>F# Query Test API</title>
///     <description>F# Query API with samples for permissions, documentation and function definitions</description>
///     <termsOfService url="https://www.coflo.ws"/>
///     <contact name="Arturo Rodriguez" url="https://www.coflo.ws" email="arturo@coflo.ws"/>
///     <license name="Apache 2.0" url="https://www.apache.org/licenses/LICENSE-2.0.html"/>
/// </info>

module XXX
    
    /// <api name="Add">
    ///     <description>Function that adds two numbers</description>
    ///     <returns>returns an integer</returns>
    ///     <param name="x" type="integer">First number to add</param>
    ///     <param name="y" type="integer">Second number to add</param>
    ///     <permissions>
    ///         <group id="$WID$" permission="read"/>
    ///     </permissions>
    /// </api>
    let Add x y = x + y

     /// <api name="Permission">
    ///     <description>Function that returns a permission</description>
    ///     <returns> returns an string</returns>
    ///     <permissions>
    ///         <group id="$WID$" permission="view"/>
    ///     </permissions>
    /// </api>
    let Permission() =
        let user = QuantApp.Kernel.User.ContextUser
        let permission = QuantApp.Kernel.User.PermissionContext("$WID$")
        match permission with
        | QuantApp.Kernel.AccessType.Write -> user.FirstName + " WRITE"
        | QuantApp.Kernel.AccessType.Read -> user.FirstName + " READ"
        | QuantApp.Kernel.AccessType.View -> user.FirstName + " VIEW"
        | _ -> user.FirstName + " DENIED"
# cd QuantApp.Client
# # npm install
# # npm install -g @angular/cli
# ng build --prod --aot

# cd ../

dotnet clean CoFlows.CE.osx.sln
dotnet publish -c Release -f netcoreapp3.1 -o QuantApp.Server/obj/osx/publish QuantApp.Server/QuantApp.Server.quant.osx.csproj

# make
javac -cp jars/scalap-2.12.8.jar:jars/scala-library.jar:./QuantApp.Kernel/JVM/app/quant/clr/ ./QuantApp.Kernel/JVM/app/quant/clr/*.java ./QuantApp.Kernel/JVM/app/quant/clr/function/*.java
scalac -d ./QuantApp.Kernel/JVM -cp ./QuantApp.Kernel/JVM/ ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.scala
jar -cf app.quant.clr.jar -C ./QuantApp.Kernel/JVM/ .
rm ./QuantApp.Kernel/JVM/app/quant/clr/*.class
rm ./QuantApp.Kernel/JVM/app/quant/clr/function/*.class
rm ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.class
mv app.quant.clr.jar ./QuantApp.Server/obj/osx/publish

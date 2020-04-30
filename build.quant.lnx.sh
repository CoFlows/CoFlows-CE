cd CoFlows.Client/Quant
# npm install
# npm install -g @angular/cli
ng build --prod --aot

cd ../../

dotnet clean CoFlows.Quant.lnx.sln
dotnet publish -c Release -f netcoreapp3.1 -o CoFlows.Server/obj/lnx/publish CoFlows.Server/CoFlows.Server.quant.lnx.csproj

javac -cp jars/scalap-2.12.8.jar:jars/scala-library.jar:./QuantApp.Kernel/JVM/app/quant/clr/ ./QuantApp.Kernel/JVM/app/quant/clr/*.java ./QuantApp.Kernel/JVM/app/quant/clr/function/*.java
scalac -d ./QuantApp.Kernel/JVM -cp ./QuantApp.Kernel/JVM/ ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.scala
jar -cf app.quant.clr.jar -C ./QuantApp.Kernel/JVM/ .
rm ./QuantApp.Kernel/JVM/app/quant/clr/*.class
rm ./QuantApp.Kernel/JVM/app/quant/clr/function/*.class
rm ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.class
mv app.quant.clr.jar ./CoFlows.Server/obj/lnx/publish
cp ./QuantApp.Kernel/JVM/JNIWrapper.cpp ./CoFlows.Server/obj/lnx/publish/
cp ./QuantApp.Kernel/JVM/app_quant_clr_CLRRuntime.h ./CoFlows.Server/obj/lnx/publish/

cd CoFlows.Server

docker build -t coflows/quant -f Dockerfile.quant .
docker tag coflows/quant coflows/quant:latest
docker push coflows/quant:latest
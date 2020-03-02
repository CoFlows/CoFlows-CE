cd QuantApp.Client
# npm install
# # npm install -g @angular/cli
ng build --prod --aot

cd ../

dotnet clean CoFlows.CE.osx.sln
dotnet publish -c Release -f netcoreapp3.1 -o QuantApp.Server/obj/osx/publish QuantApp.Server/QuantApp.Server.osx.csproj

javac -cp jars/scalap-2.12.8.jar:jars/scala-library.jar:./QuantApp.Kernel/JVM/app/quant/clr/ ./QuantApp.Kernel/JVM/app/quant/clr/*.java ./QuantApp.Kernel/JVM/app/quant/clr/function/*.java
scalac -d ./QuantApp.Kernel/JVM -cp ./QuantApp.Kernel/JVM/ ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.scala
jar -cf app.quant.clr.jar -C ./QuantApp.Kernel/JVM/ .
rm ./QuantApp.Kernel/JVM/app/quant/clr/*.class
rm ./QuantApp.Kernel/JVM/app/quant/clr/function/*.class
rm ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.class
cp app.quant.clr.jar ./QuantApp.Server/jars
mv app.quant.clr.jar ./QuantApp.Server/obj/osx/publish
cp ./QuantApp.Kernel/JVM/JNIWrapper.cpp ./QuantApp.Server/obj/osx/publish/
cp ./QuantApp.Kernel/JVM/app_quant_clr_CLRRuntime.h ./QuantApp.Server/obj/osx/publish/

cd QuantApp.Server
g++ -shared -o libJNIWrapper.jnilib  -I/Library/Java/JavaVirtualMachines/adoptopenjdk-8.jdk/Contents/Home/include -I/Library/Java/JavaVirtualMachines/adoptopenjdk-8.jdk/Contents/Home/include/darwin ../QuantApp.Kernel/JVM/JNIWrapper.cpp -fPIC -lpthread
ln -s /anaconda3/lib/libpython3.7m.dylib .
ln -s /anaconda3/bin/python3.7m .

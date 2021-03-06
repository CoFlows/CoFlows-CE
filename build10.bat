cd CoFlows.CE.Client
REM npm install
REM npm install -g @angular/cli
REM ng build --prod --aot

cd ../

dotnet clean CoFlows.CE.win.sln
dotnet publish -c Release -f netcoreapp3.1 -o CoFlows.Server/obj/win/publish CoFlows.Server/CoFlows.Server.win.csproj

REM make
javac -cp "jars/scalap-2.12.8.jar;jars/scala-library.jar;QuantApp.Kernel/JVM/app/quant/clr/" ./QuantApp.Kernel/JVM/app/quant/clr/*.java ./QuantApp.Kernel/JVM/app/quant/clr/function/*.java
scalac -d ./QuantApp.Kernel/JVM -cp ./QuantApp.Kernel/JVM/ ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.scala
jar -cf app.quant.clr.jar -C ./QuantApp.Kernel/JVM/ .
rm ./QuantApp.Kernel/JVM/app/quant/clr/*.class
rm ./QuantApp.Kernel/JVM/app/quant/clr/function/*.class
rm ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.class
mv app.quant.clr.jar ./CoFlows.Server/obj/win/publish
cp ./QuantApp.Kernel/JVM/JNIWrapper.cpp ./CoFlows.Server/obj/win/publish/
cp ./QuantApp.Kernel/JVM/app_quant_clr_CLRRuntime.h ./CoFlows.Server/obj/win/publish/


cd CoFlows.Server

docker build -t coflows/ce-win10 -f Dockerfile.win10 .
docker tag coflows/ce-win10 coflows/ce-win10:latest
docker push coflows/ce-win10:latest

REM g++ -D__int64="long long" -shared -I"C:\Program Files\Java\jdk1.8.0_221\include" -I"C:\Program Files\Java\jdk1.8.0_221\include\win32" -fPIC ..\QuantApp.Kernel\JVM\JNIWrapper.cpp -o JNIWrapper.dll
REM g++ -D__int64="long long" -shared -o JNIWrapper.dll -L"C:\Program Files\Java\jdk1.8.0_221\jre\bin\server\jvm.dll" -I"C:\Program Files\Java\jdk1.8.0_221\include" -I"C:\Program Files\Java\jdk1.8.0_221\include\win32" -fPIC ..\QuantApp.Kernel\JVM\JNIWrapper.cpp
REM g++ -D__int64="long long" -shared -o JNIWrapper.dll -L"C:\Program Files\Java\jdk1.8.0_221\jre\bin\server\jvm.dll" -I"C:\Program Files\Java\jdk1.8.0_221\include" -I"C:\Program Files\Java\jdk1.8.0_221\include\win32" -fPIC ..\QuantApp.Kernel\JVM\JNIWrapper.cpp

REM x86_64-w64-mingw32-g++ -shared -o JNIWrapper.dll -L"C:\Program Files\Java\jdk1.8.0_221\jre\bin\server\jvm.dll" -I"C:\Program Files\Java\jdk1.8.0_221\include" -I"C:\Program Files\Java\jdk1.8.0_221\include\win32" -fPIC ..\QuantApp.Kernel\JVM\JNIWrapper.cpp
REM x86_64-w64-mingw32-g++ -shared -o JNIWrapper.dll -L"C:\Program Files\Java\jdk1.8.0_221\jre\bin\server" -I"C:\Program Files\Java\jdk1.8.0_221\include" -I"C:\Program Files\Java\jdk1.8.0_221\include\win32" -fPIC ..\QuantApp.Kernel\JVM\JNIWrapper.cpp
REM x86_64-w64-mingw32-g++ -D__int64="long long" -shared -I"C:\Program Files\Java\jdk1.8.0_221\include" -I"C:\Program Files\Java\jdk1.8.0_221\include\win32" -fPIC ..\QuantApp.Kernel\JVM\JNIWrapper.cpp -o JNIWrapper.dll

REM setup-x86_64.exe -q --packages=gcc-g++

REM g++ -D__int64="long long" -shared -I"C:\openjdk-8\include" -I"C:\openjdk-8\include\win32" -ldl -fPIC JNIWrapper.cpp -o JNIWrapper.dll

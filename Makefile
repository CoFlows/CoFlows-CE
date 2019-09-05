all: java c
	
java:
	javac -cp jars/scalap-2.12.8.jar:jars/scala-library.jar:./QuantApp.Kernel/JVM/app/quant/clr/ ./QuantApp.Kernel/JVM/app/quant/clr/*.java
	scalac -d ./QuantApp.Kernel/JVM -cp ./QuantApp.Kernel/JVM/ ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.scala
	jar -cf app.quant.clr.jar -C ./QuantApp.Kernel/JVM/ .
	rm ./QuantApp.Kernel/JVM/app/quant/clr/*.class
	rm ./QuantApp.Kernel/JVM/app/quant/clr/scala/*.class
	mv app.quant.clr.jar ./QuantApp.Server/obj/Docker/publish
	
c:
	cp ./QuantApp.Kernel/JVM/JNIWrapper.cpp ./QuantApp.Server/obj/Docker/publish/

	cp ./QuantApp.Kernel/JVM/app_quant_clr_CLRRuntime.h ./QuantApp.Server/obj/Docker/publish/
	
# scala:
# 	scalac -feature *.scala
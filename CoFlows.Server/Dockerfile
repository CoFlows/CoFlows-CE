FROM mcr.microsoft.com/dotnet/aspnet:6.0

WORKDIR /app

# install python and wheels
ENV DEBIAN_FRONTEND noninteractive
RUN apt-get update && apt-get install -y --no-install-recommends apt-utils

RUN apt-get update --fix-missing && \
    apt-get install -y wget bzip2 ca-certificates libglib2.0-0 libxext6 libsm6 libxrender1 libglib2.0-dev libcairo2 libpango-1.0-0 libpangocairo-1.0-0 libgdk-pixbuf2.0-0 shared-mime-info

RUN apt-get install -y build-essential
RUN apt-get install -y libreadline-dev libncursesw5-dev libssl-dev libsqlite3-dev tk-dev libgdbm-dev libc6-dev libbz2-dev libffi-dev liblzma-dev
RUN ln -s /lib/x86_64-linux-gnu/libdl.so.2 /lib/x86_64-linux-gnu/libdl.so

RUN apt-get install -y libomp-dev curl



# Python 3.7 Install
RUN cd /usr/src && \
    wget https://www.python.org/ftp/python/3.7.9/Python-3.7.9.tgz && \
    tar xzf Python-3.7.9.tgz && \
    cd Python-3.7.9 && \
    ./configure --enable-optimizations --enable-shared && \
    make install

RUN ln -s /usr/local/bin/python3.7 python

# Java Install 11
ENV JAVA_HOME /usr/java/openjdk-11
ENV PATH $JAVA_HOME/bin:$PATH

ENV JAVA_URL https://github.com/AdoptOpenJDK/openjdk11-upstream-binaries/releases/download/jdk-11.0.13%2B7/OpenJDK11U-jdk_x64_linux_11.0.13_7_ea.tar.gz

RUN set -eux; \
	\
	curl -fL -o /openjdk.tgz "$JAVA_URL"; \
	mkdir -p "$JAVA_HOME"; \
	tar --extract --file /openjdk.tgz --directory "$JAVA_HOME" --strip-components 1; \
	rm /openjdk.tgz; \
    ln -s "${JAVA_HOME}/lib/server/"*.so /usr/local/lib/

# # Java Install 12
# ENV JAVA_HOME /usr/java/openjdk-12
# ENV PATH $JAVA_HOME/bin:$PATH
# ENV JAVA_VERSION 12

# ENV JAVA_URL https://download.java.net/java/GA/jdk12/GPL/openjdk-12_linux-x64_bin.tar.gz
# ENV JAVA_SHA256 b43bc15f4934f6d321170419f2c24451486bc848a2179af5e49d10721438dd56

# RUN set -eux; \
# 	\
# 	curl -fL -o /openjdk.tgz "$JAVA_URL"; \
# 	echo "$JAVA_SHA256 */openjdk.tgz" | sha256sum -c -; \
# 	mkdir -p "$JAVA_HOME"; \
# 	tar --extract --file /openjdk.tgz --directory "$JAVA_HOME" --strip-components 1; \
# 	rm /openjdk.tgz; \
#     ln -s "${JAVA_HOME}/lib/server/"*.so /usr/local/lib/

# Java Install 8
# ENV JAVA_HOME /usr/java/openjdk-8
# ENV PATH $JAVA_HOME/bin:$PATH

# ENV JAVA_URL https://github.com/AdoptOpenJDK/openjdk8-upstream-binaries/releases/download/jdk8u212-b04/OpenJDK8U-x64_linux_8u212b04.tar.gz

# RUN set -eux; \
# 	\
# 	curl -fL -o /openjdk.tgz "$JAVA_URL"; \
# 	mkdir -p "$JAVA_HOME"; \
# 	tar --extract --file /openjdk.tgz --directory "$JAVA_HOME" --strip-components 1; \
# 	rm /openjdk.tgz; \
#     ln -s "${JAVA_HOME}/jre/lib/amd64/server/"*.so /usr/local/lib/

# Install Scala
ENV SCALA_VERSION 2.12.10
ENV SCALA_HOME /usr/share/scala

RUN \
    cd "/tmp" && \
    wget "https://downloads.typesafe.com/scala/${SCALA_VERSION}/scala-${SCALA_VERSION}.tgz" && \
    tar xzf "scala-${SCALA_VERSION}.tgz" && \
    mkdir "${SCALA_HOME}" && \
    rm "/tmp/scala-${SCALA_VERSION}/bin/"*.bat && \
    mv "/tmp/scala-${SCALA_VERSION}/bin" "/tmp/scala-${SCALA_VERSION}/lib" "${SCALA_HOME}" && \
    ln -s "${SCALA_HOME}/bin/"* "/usr/bin/" && \
    rm -rf "/tmp/"*

# Install Spark
ENV SPARK_HOME /usr/share/spark

RUN \
    cd "/tmp" && \
    wget "https://archive.apache.org/dist/spark/spark-3.1.2/spark-3.1.2-bin-hadoop3.2.tgz" && \
    tar xzf "spark-3.1.2-bin-hadoop3.2.tgz" && \
    mkdir "${SPARK_HOME}" && \
    mv "/tmp/spark-3.1.2-bin-hadoop3.2"/* "${SPARK_HOME}" && \
    ln -s "${SPARK_HOME}/bin/"* "/usr/bin/" && \
    rm -rf "/tmp/"*


# set up network
ENV ASPNETCORE_PKG_VERSION 5.0

# SSL
# ENV ASPNETCORE_URLS https://+:443
# EXPOSE 443/tcp

# No SSL
ENV ASPNETCORE_URLS http://+:80
EXPOSE 80/tcp

COPY ${source:-obj/lnx/publish} ./

RUN mv /app/jupyter/lab /app/
RUN mv /app/jupyter/coflows /app/
RUN mv /app/jupyter/ipykernel_launcher.py /app/

RUN chmod +x /app/lab
RUN chmod +x /app/coflows

# RUN g++ -shared -o libJNIWrapper.so -L/usr/java/openjdk-12/lib/server  -I/usr/java/openjdk-12/include -I/usr/java/openjdk-12/include/linux JNIWrapper.cpp -ljvm -fPIC  -ldl -lpthread
# RUN g++ -shared -o libJNIWrapper.so  -I/usr/java/openjdk-8/include -I/usr/java/openjdk-8/include/linux JNIWrapper.cpp -fPIC -lpthread
RUN g++ -shared -o libJNIWrapper.so  -I/usr/java/openjdk-11/include -I/usr/java/openjdk-11/include/linux JNIWrapper.cpp -fPIC -lpthread


RUN \
    mkdir /app/jars && \
    cp /app/app.quant.clr.jar /app/jars && \
    cp /usr/share/scala/lib/*.jar /app/jars

RUN mkdir /app/mnt/
RUN mkdir /app/mnt/pip/

ENV LD_LIBRARY_PATH /usr/local/lib/
ENV PYTHONPATH $PATH:/usr/local/lib/python3.7:/app/mnt/pip/

RUN curl https://bootstrap.pypa.io/get-pip.py -o get-pip.py
RUN python3 get-pip.py

# Install Jupyter Notebook
RUN pip install tornado==5.1.1
RUN pip install jupyter
RUN pip install jupyterlab
RUN pip install dash
RUN pip install dash-daq
RUN pip install requests

RUN apt-get install -y unixodbc-dev
RUN pip install pyodbc

RUN mv /app/jupyter/kernel.jupyter /usr/local/share/jupyter/kernels/python3/kernel.json

RUN apt-get update && apt-get install -y procps
RUN apt-get install -y git

RUN apt-get install -y libpq-dev sudo tzdata

ENV PATH /app:$PATH

ENTRYPOINT ["dotnet", "CoFlows.Server.lnx.dll"]
cd ..\..\..
docker run -v %cd%/mnt:/App/mnt -p 80:80 quantapp/coflows-quant server
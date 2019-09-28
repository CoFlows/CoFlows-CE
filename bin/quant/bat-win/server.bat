cd ..\..\..
docker run --volume %cd%:c:/App/mnt -p 80:80 quantapp/coflows-quant-win server
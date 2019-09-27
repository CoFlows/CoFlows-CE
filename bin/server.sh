cd ..
docker run -v $(pwd)/mnt:/App/mnt -p 80:80 quantapp/coflows-ce server
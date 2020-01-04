cd ..
docker run -v $(pwd)/mnt:/app/mnt -p 80:80 coflows/ce server
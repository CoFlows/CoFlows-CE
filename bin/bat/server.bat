cd ..\..
docker run -v %cd%/mnt:/app/mnt -p 80:80 coflows/ce server
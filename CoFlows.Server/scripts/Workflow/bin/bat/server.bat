cd ..\..
docker run -v %cd%:/app/mnt -p 80:80 --env "config_file=%1" coflows/ce server
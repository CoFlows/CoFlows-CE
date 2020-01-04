cd ..\..
docker run --volume %cd%/mnt:c:/app/mnt -p 80:80 coflows/ce-win server
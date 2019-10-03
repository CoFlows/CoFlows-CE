cd ..\..
docker run --volume %cd%/mnt:c:/App/mnt -p 80:80 coflows/ce-win server
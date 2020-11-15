cd ..\..
docker run -v %cd%:/app/mnt --env "config_file=%1" coflows/ce cloud remove
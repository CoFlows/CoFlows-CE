cd ..
cd ..
docker run -v %cd%:/app/mnt --env "config_file=%2" coflows/ce %1 build
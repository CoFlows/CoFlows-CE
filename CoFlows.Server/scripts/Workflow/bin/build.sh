cd ..
docker run -v $(pwd):/app/mnt --env "config_file=$2" coflows/ce $1 build

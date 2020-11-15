cd ..
docker run -v $(pwd):/app/mnt --env "config_file=$1" coflows/ce $2 query $3 $4 $5 $6 $7 $8 $9
cd ..
# docker run -v $(pwd):/app/mnt -p 80:80 --env "coflows_config=$coflows_config" --env "coflows_package=$coflows_package" coflows/quant server
docker run -v $(pwd):/app/mnt -p 80:80 --env "config_file=$1" coflows/ce server
cd ../..
docker run -v $(pwd)/mnt:/App/mnt quantapp/coflows-quant $1 build
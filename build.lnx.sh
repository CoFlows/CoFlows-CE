# cd QuantApp.Client
# npm install
# npm install -g @angular/cli
# ng build --prod --aot

# cd ../

dotnet clean CoFlows.Quant.sln
dotnet publish -c Release -f netcoreapp3.0 -o QuantApp.Server/obj/Docker/publish QuantApp.Server/QuantApp.Server.csproj

make

cd QuantApp.Server

docker build -t coflows/quant .
docker tag coflows/quant quantapp/coflows:quant
# docker push quantapp/coflows:quant
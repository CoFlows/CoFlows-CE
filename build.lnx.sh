# cd QuantApp.Client
# # npm install
# # npm install -g @angular/cli
# ng build --prod --aot

# cd ../

dotnet clean CoFlows.CE.lnx.sln
dotnet publish -c Release -f netcoreapp3.0 -o QuantApp.Server/obj/Docker/publish QuantApp.Server/QuantApp.Server.lnx.csproj

make

cd QuantApp.Server

docker build -t coflows/ce .
docker tag coflows/ce quantapp/coflows:ce
# docker push quantapp/coflows:ce
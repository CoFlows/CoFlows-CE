cd QuantApp.Client
REM npm install
REM npm install -g @angular/cli
ng build --prod --aot

cd ../

dotnet clean CoFlows.Quant.win.sln
dotnet publish -c Release -f netcoreapp3.1 -o QuantApp.Server/obj/win/publish QuantApp.Server/QuantApp.Server.quant.win.csproj

REM make

cd QuantApp.Server

docker build -t coflows/quant-win -f Dockerfile.quant.win .
docker tag coflows/quant-win quantapp/coflows-quant-win:latest
docker push quantapp/coflows-quant-win:latest
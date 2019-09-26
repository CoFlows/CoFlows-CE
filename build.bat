cd QuantApp.Client
REM npm install
REM npm install -g @angular/cli
ng build --prod --aot

cd ../

dotnet clean CoFlows.CE.win.sln
dotnet publish -c Release -f netcoreapp3.0 -o QuantApp.Server/obj/Docker/publish QuantApp.Server/QuantApp.Server.win.csproj

REM make

cd QuantApp.Server

docker build -t coflows/ce-win -f Dockerfile.win .
docker tag coflows/ce-win quantapp/coflows:ce-win
docker push quantapp/coflows:ce-win
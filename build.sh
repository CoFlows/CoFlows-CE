cd QuantApp.Client
npm install
npm install -g @angular/cli
ng build --prod --aot

cd ../

dotnet clean CoFlows.Quant.sln
dotnet publish -c Release -f netcoreapp2.2 -o obj/Docker/publish CoFlows.Quant.sln

make

cd QuantApp.Server

docker build -t coflows/quant .
# docker tag coflows/quant quantapp/coflows:quant
# docker push quantapp/coflows:quant
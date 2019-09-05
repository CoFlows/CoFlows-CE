cd QuantApp.Client
# # npm install
# # npm install -g @angular/cli
# ng build --prod --aot

cd ../

dotnet clean CoFlows.CE.sln
dotnet publish -c Release -f netcoreapp2.2 -o obj/Docker/publish CoFlows.CE.sln

make

cd QuantApp.Server

docker build -t coflows/ce .
docker tag coflows/ce quantapp/coflows:ce
# docker push quantapp/coflows:ce
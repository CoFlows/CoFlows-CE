# cd QuantApp.Client
# # npm install
# # npm install -g @angular/cli
# ng build --prod --aot

# cd ../

dotnet clean CoFlows.CE.osx.sln
dotnet publish -c Release -f netcoreapp3.0 -o QuantApp.Server/obj/Docker/publish QuantApp.Server/QuantApp.Server.osx.csproj

make

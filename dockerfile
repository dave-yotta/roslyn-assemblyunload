FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim

RUN curl -sSL https://dot.net/v1/dotnet-install.sh > dotnet-install.sh
RUN chmod u+x dotnet-install.sh
RUN ./dotnet-install.sh --channel 3.1 --install-dir /usr/share/dotnet

RUN apt-get update && apt-get -y install lsof

COPY . /roslyn-assemblyunload-build

WORKDIR /roslyn-assemblyunload-build

RUN dotnet publish roslyn-assemblyunload-refnetstd/roslyn-assemblyunload-refnetstd.csproj -o /roslyn-assemblyunload-refnetstd

CMD ["dotnet", "/roslyn-assemblyunload-refnetstd/roslyn-assemblyunload-refnetstd.dll"]
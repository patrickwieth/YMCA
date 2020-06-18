FROM mono:6.8.0.96

RUN mkdir /workingdir

WORKDIR /workingdir

RUN apt-get update && apt-get install -y ca-certificates && apt-get -y upgrade

RUN apt install -y --allow-unauthenticated --no-install-recommends mono-devel zip file libfreetype6 libopenal1 liblua5.1-0 libsdl2-2.0-0 xdg-utils zenity wget make unzip python nsis imagemagick

#RUN dotnet --version

#RUN mkdir output && packaging/package-all.sh yes-version output/

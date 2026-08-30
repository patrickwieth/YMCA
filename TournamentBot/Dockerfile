FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build

ARG YMCA_VERSION=v0.96.16

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl make python3 unzip \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Keep the expensive engine build in a cache layer that only changes when
# mod.config selects a new engine revision.
COPY mod.config fetch-engine.sh ./
RUN ./fetch-engine.sh \
    && make -C engine TARGETPLATFORM=linux-x64 all

COPY . .
RUN make version VERSION="${YMCA_VERSION}" \
    && dotnet build YMCA.sln --configuration Release \
    && dotnet publish TournamentBot/TournamentBot.csproj \
        --configuration Release \
        --output /out/bot \
        --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates libfontconfig1 libfreetype6 libgl1 libopenal1 libsdl2-2.0-0 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /out/bot /app/bot
COPY --from=build /src/engine/bin /app/game/engine/bin
COPY --from=build /src/engine/glsl /app/game/engine/glsl
COPY --from=build /src/engine/mods /app/game/engine/mods
COPY --from=build /src/mods /app/game/mods
COPY --from=build /src/tools /app/game/tools

RUN useradd --create-home --uid 1000 ymca \
    && mkdir -p /data/matches \
    && chown -R ymca:ymca /data /app

USER ymca

EXPOSE 5080 12400-12407
ENTRYPOINT ["dotnet", "/app/bot/TournamentBot.dll", "/config/tournament-bot.json"]

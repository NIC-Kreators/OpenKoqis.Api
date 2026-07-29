# Contributing to the OpenKoqis Guideline

## 🚀 Development Environment (Aspire)

This is the way to run the project day-to-day. The section below on container building is for producing a publishable image, not for local development — you don't need Docker builds or a `Dockerfile` just to work on the code.

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or newer)
* Docker Desktop (or a running Docker daemon)

### First-time setup
Set the MongoDB dev password once:

```bash
dotnet user-secrets set "Parameters:mongo-password" "your-dev-password" --project src/OpenKoqis.Api.AppHost
```

The username defaults to `develop_admin` (see `src/OpenKoqis.Api.AppHost/appsettings.Development.json`); override it the same way if needed.

### Running the app WITH infrastructure
Open the solution in your IDE (Rider/VS/VS Code) and run the `OpenKoqis.Api.AppHost` project.

Prefer the terminal? Install the [Aspire CLI](https://aspire.dev/docs/cli/install/) and run `aspire run` from the repository root instead.

This starts:
* `OpenKoqis.Api.AppHost` — the orchestrator, which starts and wires together everything below
* `OpenKoqis.Api` — the API project, running directly (not containerized) for fast iteration/hot reload
* MongoDB — as a container, with a persistent data volume
* Mosquitto (MQTT broker) — as a container, using the config under `config/mosquitto/`

The AppHost prints a link to the **Aspire dashboard**, where you can see logs, traces, metrics, and resource status for everything above — you don't need Seq/Prometheus/Grafana locally, the dashboard covers OpenTelemetry viewing for development.

> Note: the Docker-based build below (`dotnet publish ... /t:PublishContainer`) is for producing a release artifact, not for development — always use Aspire locally.

## 🐳 Container Building (No Dockerfile Required)

This project leverages native .NET container support. You do not need a `Dockerfile` to build or package this application into a container image.

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (or newer)
* [Docker Desktop](https://docker.com) or a running Docker daemon

### How to Build the Container Image
Run the following standard .NET CLI command in the root folder to build and publish the application directly to your local Docker daemon:

```bash
dotnet publish --os linux --arch x64 -c Release /t:PublishContainer
```

or for ARM machines like Apple Silicon:

```bash
dotnet publish --os linux --arch arm64 -c Release /t:PublishContainer
```

> Note: use only `-c Release` as for development purpose Aspire is using

### Running Your Container
Once the build completes, you can run your newly created image locally using standard Docker commands:

```bashs
docker run -d -p 8080:8080 --name open-koqis.api open-koqis.api
```

### Why no Dockerfile?
We use the official Microsoft built-in container support. This reduces repository maintenance, keeps base images automatically updated with the SDK, and simplifies cross-platform targeting (`--os linux --arch x64`) directly from the command line.

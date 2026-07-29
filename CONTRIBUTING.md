# Contributing to the OpenKoqis Guideline

## 🐳 Container Building (No Dockerfile Required)

This project leverages native .NET container support. You do not need a `Dockerfile` to build or package this application into a container image.

### Prerequisites
* [.NET 8.0 SDK](https://microsoft.com) (or newer)
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

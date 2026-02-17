# Identity
A service which is responsible for identity management (authentication, credentials, etc.)

## Cookbook

### How to prepare the environment
- Install Docker: https://docs.docker.com/engine/install/
- Install Docker Compose Plugin: https://docs.docker.com/compose/install/linux/

### How to run the project?
```shell
docker compose up
```
When you need to stop the project, just use `Ctrl+C`

### How to destroy the project?
```shell
docker compose down --remove-orphans --volumes --rmi local
```
The command completely removes images, containers, volumes and networks.
It might be helpful when you need to clear all data and run the project from scratch

### How to run tests?
```shell
docker compose run --rm identity dotnet test
docker compose run --rm identity dotnet test src/Infrastructure.Test
```

### How to run formatting?
```shell
docker compose run --rm identity dotnet format
```

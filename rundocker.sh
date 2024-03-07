. ./setenv.sh
docker run --rm --name my_service --env-file env.list -e DatabaseName=vista service:latest

solution_root := justfile_directory()

_default:
    @just --list

# Run frontend
start:
    cd {{ solution_root }}/src/Movie.Web && pnpm run start

# Run application
[arg('profile', pattern='^(https|http)$')]
run profile='https':
    dotnet run --project {{ solution_root }}/src/Api --launch-profile {{ profile }}

# Watch application
[arg('profile', pattern='^(https|http)$')]
watch profile='https':
    DOTNET_WATCH_SUPPRESS_STATIC_FILE_HANDLING=1 \
    dotnet watch run --quiet --project {{ solution_root }}/src/Api --launch-profile {{ profile }}

# format code
fmt:
    dotnet csharpier format .

# docker up
[arg('profile', pattern='^(dev|test)$')]
dup profile='dev':
    #!/usr/bin/env bash
    set -euo pipefail

    docker compose -f {{ solution_root }}/docker/compose.yml --profile {{ profile }} up -d

    if [[ "{{ profile }}" == "test" ]]; then
      until docker exec test_pg pg_isready -U $DATABASE_TEST_USER -d $DATABASE_TEST_NAME; do
        sleep 1
      done
      dbmate --url $DATABASE_TEST_URL up
    fi

# docker down
[arg('profile', pattern='^(dev|test)$')]
dwn profile='dev':
    docker compose -f {{ solution_root }}/docker/compose.yml --profile {{ profile }} down

# run test
[arg('filter', help='Test targets like "FullyQualifiedName~ProductServiceTests"')]
test filter='':
    #!/usr/bin/env bash

    # docker up
    docker compose -f {{ solution_root }}/docker/compose.yml --profile test up -d

    # apply migration
    until docker exec test_pg pg_isready -U $DATABASE_TEST_USER -d $DATABASE_TEST_NAME; do
      sleep 1
    done
    dbmate --url $DATABASE_TEST_URL up

    cleanup() {
      docker compose -f {{ solution_root }}/docker/compose.yml --profile test down
    }
    trap cleanup EXIT

    DOTNET_TEST_ARGS=()

    if [[ -n "{{ filter }}" ]]; then
      DOTNET_TEST_ARGS+=(--filter "{{ filter }}")
    fi

    # run tests
    ASPNETCORE_ENVIRONMENT=Test \
    DATABASE__HOST=localhost \
    DATABASE__PORT=$DATABASE_TEST_PORT \
    DATABASE__USER=$DATABASE_TEST_USER \
    DATABASE__PASSWORD=$DATABASE_TEST_PASSWORD \
    DATABASE__NAME=$DATABASE_TEST_NAME \
    DATABASE__SSLMODE=$DATABASE_TEST_SSLMODE \
    dotnet test "${DOTNET_TEST_ARGS[@]}"

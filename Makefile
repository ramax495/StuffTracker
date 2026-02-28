# StuffTracker — Project Makefile
# Polyglot project: .NET 8 backend + Angular 20 frontend + PostgreSQL
# Usage: make help

SHELL := bash
.ONESHELL:
.SHELLFLAGS := -eu -o pipefail -c
.DELETE_ON_ERROR:
MAKEFLAGS += --warn-undefined-variables
MAKEFLAGS += --no-builtin-rules

# --- Project ---
PROJECT  := stufftracker
BACKEND  := backend
FRONTEND := frontend
SOLUTION := $(BACKEND)/StuffTracker.sln
API_PROJ := $(BACKEND)/src/StuffTracker.Api/StuffTracker.Api.csproj

# --- Git ---
VERSION    ?= $(shell git describe --tags --always --dirty 2>/dev/null || echo "dev")
COMMIT     ?= $(shell git rev-parse --short HEAD 2>/dev/null || echo "unknown")
BUILD_TIME := $(shell date -u '+%Y-%m-%dT%H:%M:%SZ')

# --- Frontend test browser ---
# Override for headless: make frontend-test BROWSERS=ChromeHeadless
BROWSERS ?= Chrome

# --- Container runtime ---
# Auto-detect compose command: podman compose > docker compose > docker-compose
# Override: make dev COMPOSE="docker-compose"
COMPOSE ?= $(shell \
  if command -v podman >/dev/null 2>&1 && podman compose version >/dev/null 2>&1; then \
    echo "podman compose"; \
  elif docker compose version >/dev/null 2>&1; then \
    echo "docker compose"; \
  elif command -v docker-compose >/dev/null 2>&1; then \
    echo "docker-compose"; \
  else \
    echo "docker compose"; \
  fi)

# --- Docker ---
DOCKER_REGISTRY ?= ghcr.io
DOCKER_IMAGE    ?= $(DOCKER_REGISTRY)/$(PROJECT)
DOCKER_TAG      ?= $(VERSION)

# Load .env if present (non-fatal)
-include .env

# ============================================================================
.DEFAULT_GOAL := help

##@ Setup

.PHONY: setup
setup: ## First-time project setup: restore deps, start DB, run migrations
	dotnet restore $(SOLUTION)
	npm --prefix $(FRONTEND) ci
	$(COMPOSE) up postgres -d --wait
	cd $(BACKEND) && ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --project src/StuffTracker.Api

.PHONY: tools-install
tools-install: ## Install required .NET global tools (dotnet-ef)
	dotnet tool install --global dotnet-ef 2>/dev/null || dotnet tool update --global dotnet-ef

##@ Development

.PHONY: dev
dev: infra-up ## Start full local dev: DB + backend watcher + frontend serve (Ctrl+C stops all)
	trap 'kill 0' SIGINT
	(cd $(BACKEND) && dotnet watch run --project src/StuffTracker.Api) &
	(cd $(FRONTEND) && npm start) &
	wait

.PHONY: backend-dev
backend-dev: ## Start .NET backend with hot reload only (dotnet watch → http://localhost:5000)
	cd $(BACKEND) && dotnet watch run --project src/StuffTracker.Api

.PHONY: frontend-dev
frontend-dev: ## Start Angular dev server only (ng serve → http://localhost:4200)
	cd $(FRONTEND) && npm start

##@ Build

.PHONY: build
build: backend-build frontend-build ## Build all (backend publish + frontend bundle)

.PHONY: backend-build
backend-build: ## Publish .NET backend in Release mode → backend/publish/
	dotnet publish $(API_PROJ) \
		-c Release \
		-o $(BACKEND)/publish \
		/p:UseAppHost=false

.PHONY: frontend-build
frontend-build: ## Build Angular app for production → frontend/dist/stuff-tracker/
	npm --prefix $(FRONTEND) run build

##@ Testing

.PHONY: test
test: backend-test frontend-test ## Run all tests (backend unit/integration/contract + frontend)

.PHONY: backend-test
backend-test: ## Run all .NET tests (unit + integration + contract)
	dotnet test $(SOLUTION) --logger "console;verbosity=minimal"

.PHONY: frontend-test
frontend-test: ## Run Angular tests (override: make frontend-test BROWSERS=ChromeHeadless)
	npm --prefix $(FRONTEND) run test -- --watch=false --browsers=$(BROWSERS)

##@ Code Quality

.PHONY: lint
lint: fmt-check ## Run all linting checks

.PHONY: fmt
fmt: ## Format frontend source files with Prettier
	cd $(FRONTEND) && npx prettier --write .

.PHONY: fmt-check
fmt-check: ## Check frontend formatting with Prettier (fails if unformatted)
	cd $(FRONTEND) && npx prettier --check .

##@ Database

.PHONY: db-migrate
db-migrate: ## Apply all pending EF Core migrations (requires postgres running)
	cd $(BACKEND) && ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --project src/StuffTracker.Api

.PHONY: db-migration
db-migration: ## Create a new EF Core migration (usage: make db-migration NAME=AddSomeThing)
	@[ -n "$(NAME)" ] || (echo "ERROR: NAME is required  →  make db-migration NAME=<MigrationName>" && exit 1)
	cd $(BACKEND) && dotnet ef migrations add $(NAME) --project src/StuffTracker.Api

.PHONY: db-status
db-status: ## List EF Core migrations and their applied status
	cd $(BACKEND) && dotnet ef migrations list --project src/StuffTracker.Api

##@ Docker — Infrastructure

.PHONY: infra-up
infra-up: ## Start only PostgreSQL (used by dev and setup)
	$(COMPOSE) up postgres -d --wait

.PHONY: infra-down
infra-down: ## Stop the PostgreSQL container
	$(COMPOSE) stop postgres

.PHONY: infra-logs
infra-logs: ## Tail PostgreSQL container logs
	$(COMPOSE) logs -f postgres

##@ Docker — Full Stack

.PHONY: docker-build
docker-build: ## Build all Docker images (backend + frontend) via compose
	$(COMPOSE) --profile full build \
		--build-arg VERSION=$(VERSION) \
		--build-arg COMMIT=$(COMMIT)

.PHONY: docker-up
docker-up: ## Start full stack in foreground (postgres + backend + frontend)
	$(COMPOSE) --profile full up

.PHONY: docker-up-d
docker-up-d: ## Start full stack in detached mode
	$(COMPOSE) --profile full up -d

.PHONY: docker-down
docker-down: ## Stop and remove all containers
	$(COMPOSE) --profile full down

.PHONY: docker-logs
docker-logs: ## Tail logs from all running containers
	$(COMPOSE) --profile full logs -f

.PHONY: docker-push
docker-push: ## Push images to registry (requires DOCKER_REGISTRY)
	$(COMPOSE) --profile full push

.PHONY: docker-clean
docker-clean: ## Remove containers, volumes, and dangling images
	$(COMPOSE) --profile full down --remove-orphans --volumes
	docker image prune -f

##@ CI

.PHONY: ci
ci: ## Run full CI pipeline: lint → backend-test → frontend-test → build
	$(MAKE) lint
	$(MAKE) backend-test
	$(MAKE) frontend-test BROWSERS=ChromeHeadless
	$(MAKE) build

##@ Cleanup

.PHONY: clean
clean: ## Remove build artifacts (backend/publish, backend/bin/obj, frontend/dist)
	rm -rf $(BACKEND)/publish $(BACKEND)/bin $(BACKEND)/obj
	rm -rf $(FRONTEND)/dist $(FRONTEND)/.angular

.PHONY: clean-all
clean-all: clean ## Remove artifacts and frontend node_modules
	rm -rf $(FRONTEND)/node_modules

##@ Help

.PHONY: help
help: ## Show this help message
	@awk 'BEGIN {FS = ":.*##"; printf "Usage:\n  make \033[36m<target>\033[0m\n"} \
		/^[a-zA-Z_-]+:.*?## / {printf "  \033[36m%-22s\033[0m %s\n", $$1, $$2} \
		/^##@/ {printf "\n\033[1m%s\033[0m\n", substr($$0, 5)}' $(MAKEFILE_LIST)

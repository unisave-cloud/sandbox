VERSION=$$(grep -oP "AssemblyInformationalVersion\(\"\K[^\"]+" UnisaveSandbox/Properties/AssemblyInfo.cs)
TAG=registry.digitalocean.com/unisave/unisave-sandbox:$(VERSION)

.PHONY: build push run run-sh

build:
	docker build --tag $(TAG) .

push:
	docker push $(TAG)

run:
	docker run --rm -it -p 8080:8080 -p 8081:8081 -p 5000:5000 $(TAG)

run-sh:
	docker run --rm -it --entrypoint bash $(TAG)

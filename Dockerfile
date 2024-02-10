FROM alpine:latest

ARG DEPLOYABLE_VERSION
ARG TARGETPLATFORM

ENTRYPOINT ["/bin/ash", "-c", "sleep infinity"]

EXPOSE 3000
LABEL org.opencontainers.image.source=https://github.com/mars-office/huna-signalr

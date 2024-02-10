FROM alpine:latest

ARG DEPLOYABLE_VERSION
RUN echo "export const VERSION = '${DEPLOYABLE_VERSION}'" > ./src/version.ts
ARG TARGETPLATFORM

ENTRYPOINT ["/bin/ash", "-c", "sleep infinity"]

EXPOSE 3000
LABEL org.opencontainers.image.source=https://github.com/mars-office/template-deployable

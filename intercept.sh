#!/bin/sh
telepresence connect -n huna
telepresence intercept huna-huna-signalr --port 3005:http --to-pod 8181 --env-json ./env.json || true

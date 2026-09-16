FROM ollama/ollama:latest

# Expose standard Ollama REST API port
EXPOSE 11434

# Ensure default environment variables for container runtime
ENV OLLAMA_HOST=0.0.0.0:11434
ENV OLLAMA_MODELS=/root/.ollama/models

ENTRYPOINT ["ollama", "serve"]


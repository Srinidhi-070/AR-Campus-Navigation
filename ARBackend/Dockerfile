FROM python:3.11-slim

WORKDIR /app

# Prevent .pyc and enable unbuffered logs for easier debugging
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

# System deps (optional but helps common wheels)
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
  && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy app
COPY . .

# FastAPI port
EXPOSE 8000

# Uvicorn: no --reload inside container
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]

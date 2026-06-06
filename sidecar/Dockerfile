# Stage 1: build / install deps
FROM python:3.12-slim AS build
WORKDIR /app

# Install dependencies into a virtual env so we can copy only the venv to the final image
RUN python -m venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Stage 2: lean runtime image
FROM python:3.12-slim
WORKDIR /app

# Copy virtual env from build stage
COPY --from=build /opt/venv /opt/venv
ENV PATH="/opt/venv/bin:$PATH"

COPY main.py .

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]

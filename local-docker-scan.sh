#!/bin/bash
# Script de auditoría QA Senior para escaneo local de vulnerabilidades con Trivy

# Verificar si Trivy está instalado
if ! command -v trivy &> /dev/null; then
    echo "Error: Trivy no está instalado. Instálalo con: brew install aquasecurity/trivy/trivy"
    exit 1
fi

echo "Log: Iniciando Auditoría de Seguridad de Contenedores..."
RESULTS_DIR="$(pwd)/TestResults/SecurityScan"
mkdir -p "$RESULTS_DIR"

# Buscar Dockerfiles de servicios e identicarlos
DOCKERFILES=$(find ./services -name "Dockerfile" -not -path "*/node_modules/*")

for DF in $DOCKERFILES; do
    # Extraer el nombre del servicio de la ruta (e.g. ./services/catalog/src/Catalog.Api/Dockerfile -> catalog)
    SERVICE=$(echo "$DF" | cut -d'/' -f3)
    IMAGE_NAME="local/scan-$SERVICE:latest"
    
    echo ""
    echo "--------------------------------------------------------"
    echo "Auditando Servicio: $SERVICE"
    echo "Origen: $DF"
    echo "--------------------------------------------------------"
    
    # Construir imagen silenciosamente
    echo "Construyendo imagen $IMAGE_NAME..."
    docker build -t "$IMAGE_NAME" -f "$DF" . > /dev/null 2>&1
    
    if [ $? -eq 0 ]; then
        # Escanear con Trivy
        echo "Escaneando vulnerabilidades (HIGH,CRITICAL)..."
        trivy image --severity HIGH,CRITICAL --format table "$IMAGE_NAME"
        
        # Guardar reporte JSON
        trivy image --severity HIGH,CRITICAL --format json --output "$RESULTS_DIR/$SERVICE-report.json" "$IMAGE_NAME" > /dev/null 2>&1
    else
        echo "Error: Falló la construcción de la imagen para $SERVICE"
    fi
done

echo ""
echo "Log: Auditoría finalizada. Reportes detallados en $RESULTS_DIR"

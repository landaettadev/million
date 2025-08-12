# 🚀 Configuración Rápida - Million Real Estate

## ⚡ Setup en 5 Minutos

### 1. **Prerequisitos (Instalar antes)**
```bash
# .NET 8 SDK
# Descargar desde: https://dotnet.microsoft.com/download/dotnet/8.0

# Node.js 18+
# Descargar desde: https://nodejs.org/

# Docker Desktop
# Descargar desde: https://www.docker.com/
```

### 2. **Clonar y Configurar**
```bash
# Clonar repositorio
git clone https://github.com/landaettadev/million.git
cd million

# Iniciar servicios
docker-compose up -d

# Verificar que MongoDB esté ejecutándose
docker ps
```

### 3. **Configurar Backend**
```bash
cd backend/RealEstate.Api

# Copiar configuración (ya está lista)
# El archivo appsettings.Development.json ya está configurado

# Ejecutar backend
dotnet restore
dotnet run
```

### 4. **Configurar Frontend**
```bash
# En nueva terminal
cd million/frontend

# El archivo .env.local ya está configurado

# Instalar dependencias y ejecutar
npm install
npm run dev
```

### 5. **¡Listo!**
- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5244
- **Swagger**: http://localhost:5244/swagger

---

## 🔧 **Configuración Automática**

### **Backend (Ya Configurado)**
- ✅ MongoDB connection string
- ✅ JWT keys
- ✅ CORS settings
- ✅ Seed habilitado
- ✅ Videos lujosa1.mp4 y lujosa2.mp4 incluidos

### **Frontend (Ya Configurado)**
- ✅ API endpoints
- ✅ Variables de entorno
- ✅ Videos en /public

---

## 🎯 **Características Incluidas**

### **Videos de Lujosa**
- `lujosa1.mp4` - Asignado a "Luxury Penthouse Madrid"
- `lujosa2.mp4` - Asignado a "Modern Apartment Barcelona"

### **Seed Data Completo**
- 4 propietarios con fotos
- 12 propiedades con imágenes
- Imágenes usando Picsum (placeholders reales)
- Videos asignados automáticamente

### **Base de Datos**
- MongoDB con autenticación
- Colecciones: properties, owners, propertyImages
- Índices optimizados

---

## 🚨 **Solución de Problemas**

### **Error 500 - Internal Server Error**
```bash
# Verificar que MongoDB esté ejecutándose
docker ps

# Verificar que Azurite esté ejecutándose
docker ps | grep azurite

# Reiniciar servicios
docker-compose down
docker-compose up -d
```

### **Error de Conexión MongoDB**
```bash
# Verificar credenciales en appsettings.Development.json
# Usuario: admin
# Password: password123
# Database: realestate_dev
```

### **Frontend no carga**
```bash
# Verificar que el backend esté ejecutándose en puerto 5244
curl http://localhost:5244/api/properties/featured?limit=3

# Verificar variables de entorno en .env.local
```

---

## 📱 **Credenciales por Defecto**

- **Email**: admin@millionluxury.com
- **Password**: admin123

---

## 🔄 **Actualizaciones**

```bash
# Actualizar desde el repositorio
git pull origin main

# Reiniciar servicios
docker-compose down
docker-compose up -d

# Recompilar backend
cd backend/RealEstate.Api
dotnet run
```

---

## 📞 **Soporte**

Si tienes problemas:
1. Verificar que Docker esté ejecutándose
2. Verificar que los puertos 27017, 5244, 3000 estén libres
3. Revisar logs del backend
4. Verificar configuración en appsettings.Development.json

---

**¡El proyecto está configurado para funcionar inmediatamente! 🎉**

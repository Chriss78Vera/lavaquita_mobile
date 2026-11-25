# La Vaquita Mobile

Aplicación móvil .NET MAUI para dividir cuentas en modo local o mediante salas compartidas.

## Autores

- Christopher Vera
- Jordan Paillacho

## Iniciar

1. Inicia primero `lavaquita_backend`.
2. Abre `LaVaquita.slnx` en Visual Studio.
3. Selecciona Android, iOS o Windows y ejecuta el proyecto.

Android Emulator usa `http://10.0.2.2:3000`. Para teléfonos físicos cambia la URL de `MauiProgram.cs` por la URL HTTPS pública del backend.

En una sala compartida, el anfitrión puede elegir una foto de la factura en Android, iOS o Windows. Android e iOS también permiten tomarla con la cámara. En Windows, el botón Cámara abre el selector de archivos.

Para leer el total, activa Ollama siguiendo el README del backend. La app envía la foto a la API de La Vaquita y admite hasta 100 segundos de espera para el OCR. El monto manual sigue disponible.

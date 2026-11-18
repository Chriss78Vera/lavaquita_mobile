# La Vaquita Mobile

Aplicación móvil .NET MAUI para Android e iOS. El proyecto conserva la estructura utilizada en los demás ejercicios de la carpeta `AplicacionesMoviles`.

## Estructura

## Diseño visual

La interfaz adapta el mockup de Figma a controles MAUI nativos:

- Paleta crema, verde tinta, lima, turquesa, amarillo, azul y naranja.
- Tarjetas con bordes oscuros, sombras desplazadas y esquinas redondeadas.
- Estados visuales para sala en vivo, sincronización, OCR, juegos y resultados.
- Selector de avatares mediante tarjetas en lugar de un selector de texto.
- Recursos SVG propios en `Resources/Images`: mascota, factura, ruleta y dados.
- Diseño con ancho máximo y contenido desplazable para teléfonos Android y iPhone.

### Views

- `vInicio`: menú principal.
- `vModoRapido`: participantes y monto en un solo teléfono.
- `vCrearSala`: nombre/avatar del anfitrión y creación de sala.
- `vUnirseSala`: entrada por código sin registro.
- `vSala`: QR, participantes, fotografía/galería, OCR y monto manual.
- `vJuegos`: división, ruleta y dados.
- `vResultado`: resultado, pagos y desempates.

Cada pantalla tiene su archivo XAML y su code-behind.

### Models

- `Participante`: identidad temporal, avatar y rol.
- `Cuenta`: total, moneda y datos de OCR.
- `Sala`: estado completo de una sala compartida.
- `ResultadoJuego` y `DetallePago`: resultado normalizado para todos los juegos.
- `AppSession`: estado temporal compartido entre vistas.
- `AvatarOption`: catálogo de avatares.
- `OcrResult`: respuesta de la IA.
- `CredencialesSala`: tokens temporales al crear una sala.

### Services

- `VaquitaApi`: único punto de comunicación con el backend. Convierte JSON en modelos y evita que las vistas dependan del formato HTTP.

## Ejecución

Primero inicia el backend:

```powershell
cd "..\lavaquita_backend"
npm run dev
```

Después abre `LaVaquita.slnx` en Visual Studio y ejecuta Android. El emulador Android usa `http://10.0.2.2:3000`; iOS Simulator usa `http://localhost:3000`.

Para un dispositivo físico, cambia la URL de `MauiProgram.cs` por una dirección HTTPS accesible desde el teléfono.

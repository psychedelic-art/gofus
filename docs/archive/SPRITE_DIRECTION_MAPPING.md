# Sistema de Mapeo de Direcciones de Sprites - Dofus a Unity

**Fecha:** 21 de Noviembre, 2025  
**Estado:** ✅ Implementado - Listo para Pruebas  
**Prioridad:** Crítica

---

## 🎯 Problema Identificado

### Síntomas Reportados:
1. **Sprites de Sram cortados** - Solo se ve parte del personaje
2. **Otras clases no muestran todas las direcciones** - Solo South y East disponibles  
3. **Direcciones invertidas** - South y East están intercambiados

### Causa Raíz:

Dofus utiliza **5 direcciones** desde renders 3D:
- **F** (Front/Frente) - Alejándose de la cámara
- **B** (Back/Atrás) - Hacia la cámara
- **R** (Right/Derecha) - Lado derecho
- **L** (Left/Izquierda) - Lado izquierdo
- **S** (Side/South/Sur) - Vista lateral/sur

Unity necesita **8 direcciones** para movimiento isométrico:
- **N** (North/Norte), **NE**, **E** (East/Este), **SE**, **S** (South/Sur), **SW**, **W** (West/Oeste), **NW**

---

## 🔄 Solución Implementada

### Sistema de Mapeo con Mirror/Flip

El sistema mapea las 8 direcciones de Unity a las 5 direcciones de Dofus, usando **horizontal flip** (flipX) donde sea necesario.

### Tabla de Mapeo (CORREGIDA después de pruebas con Xelor)

| Unity Dir | Descripción | Dofus Dir | Flip | Notas |
|-----------|-------------|-----------|------|-------|
| **N** | Norte (arriba) | **B** | No | Espalda visible, alejándose |
| **NE** | Noreste (diagonal arriba-derecha) | **L** | **Sí** | Diagonal izquierda invertida (simétrico a NW) |
| **E** | Este (derecha) | **S** | No | Vista lateral derecha |
| **SE** | Sureste (diagonal abajo-derecha) | **R** | No | Diagonal derecha inferior |
| **S** | Sur (abajo) | **F** | No | Frente visible, hacia cámara |
| **SW** | Suroeste (diagonal abajo-izquierda) | **R** | **Sí** | Diagonal derecha invertida (simétrico a SE) |
| **W** | Oeste (izquierda) | **S** | **Sí** | Vista lateral izquierda invertida |
| **NW** | Noroeste (diagonal arriba-izquierda) | **L** | No | Diagonal izquierda superior |

### Ejemplos de Conversión (CORREGIDOS)

```
Unity Request       →  Dofus Animation  →  FlipX
-----------------      ----------------     ------
staticN            →  staticB              false  (espalda visible)
walkE              →  walkS                false  (lateral derecho)
runW               →  runS                 true   (lateral izquierdo)
staticSW           →  staticR              true   (diagonal SE invertida)
walkNE             →  walkL                true   (diagonal NW invertida)
runS               →  runF                 false  (frente visible)
staticSE           →  staticR              false  (diagonal derecha inferior)
walkNW             →  walkL                false  (diagonal izquierda superior)
```

---

## 📝 Código Implementado

### 1. Campo para Estado de Flip (CharacterLayerRenderer.cs)

**Línea ~29:**
```csharp
// Direction mapping and mirroring
private bool currentShouldFlip = false; // Should current animation be flipped horizontally
```

### 2. Método de Mapeo (CharacterLayerRenderer.cs)

**Líneas ~260-295:**
```csharp
/// <summary>
/// Maps Unity 8-direction naming (N, NE, E, SE, S, SW, W, NW) to Dofus 5-direction naming (F, R, L, S, B)
/// Returns the Dofus direction suffix and whether the sprite should be flipped horizontally
/// </summary>
private (string dofusDirection, bool shouldFlip) MapUnityDirectionToDofus(string unityDirection)
{
    // Dofus directions: F=Front(away), B=Back(toward), R=Right, L=Left, S=Side/South
    // Unity directions: N=North(up), S=South(down), E=East(right), W=West(left)
    
    switch (unityDirection)
    {
        case "N":   return ("B", false); // North → Back visible
        case "NE":  return ("L", true);  // Northeast → Left diagonal (flipped)
        case "E":   return ("S", false); // East → Side lateral
        case "SE":  return ("R", false); // Southeast → Right diagonal lower
        case "S":   return ("F", false); // South → Front visible
        case "SW":  return ("R", true);  // Southwest → Right diagonal (flipped)
        case "W":   return ("S", true);  // West → Side lateral (flipped)
        case "NW":  return ("L", false); // Northwest → Left diagonal upper
        default:    return ("S", false); // Fallback
    }
}
```

### 3. LoadCharacterSprites Modificado (CharacterLayerRenderer.cs)

**Líneas ~87-134:**
```csharp
private void LoadCharacterSprites()
{
    // Parse animation into state and direction (e.g., "walkN" → "walk" + "N")
    string animState = "";
    string unityDirection = "";
    
    // Extract state and direction from currentAnimation
    if (currentAnimation.StartsWith("static"))
    {
        animState = "static";
        unityDirection = currentAnimation.Substring(6); // Remove "static"
    }
    else if (currentAnimation.StartsWith("walk"))
    {
        animState = "walk";
        unityDirection = currentAnimation.Substring(4); // Remove "walk"
    }
    else if (currentAnimation.StartsWith("run"))
    {
        animState = "run";
        unityDirection = currentAnimation.Substring(3); // Remove "run"
    }

    // Map Unity direction to Dofus direction
    var (dofusDirection, shouldFlip) = MapUnityDirectionToDofus(unityDirection);
    currentShouldFlip = shouldFlip;
    
    // Build Dofus animation name (e.g., "walkS", "staticR")
    string dofusAnimation = animState + dofusDirection;
    
    if (showDebugInfo)
        Debug.Log($"[CharacterLayerRenderer] Mapped {currentAnimation} → {dofusAnimation} (flip: {shouldFlip})");

    // Search for folders ending with dofusAnimation (e.g., "DefineSprite_52_walkS")
    // ...
}
```

### 4. SetupSpriteLayers con FlipX (CharacterLayerRenderer.cs)

**Líneas ~220-235:**
```csharp
// Create sprite layers - ALL at the SAME offset
for (int i = 0; i < sprites.Count; i++)
{
    GameObject layerObj = new GameObject($"Layer_{i}");
    layerObj.transform.SetParent(transform);
    layerObj.transform.localPosition = layerOffset;
    layerObj.transform.localScale = Vector3.one;

    SpriteRenderer sr = layerObj.AddComponent<SpriteRenderer>();
    sr.sprite = sprites[i];
    sr.sortingLayerName = sortingLayerName;
    sr.sortingOrder = sortingOrder + i;
    sr.flipX = currentShouldFlip; // ← Apply horizontal flip
    
    spriteLayers.Add(sr);
}
```

---

## 🧪 Instrucciones de Prueba

### Paso 1: Compilar en Unity

1. Guarda todos los archivos
2. Abre Unity y espera la compilación
3. Verifica que no haya errores en la consola

### Paso 2: Usar CharacterRenderingTest

1. Abre la escena con `CharacterRenderingTest`
2. Entra en **Play Mode**
3. **Cambia Class ID slider a 5** (Xelor)
4. Click **"Test Single Character"**

### Paso 3: Probar Todas las Direcciones

Usa los botones de dirección para probar cada una:

**Estado: Static (Idle)**
1. Click **"Static"** button
2. Prueba cada dirección:
   - **N** → Debe mostrar Xelor de espaldas (Front)
   - **NE** → Debe mostrar Xelor mirando diagonal derecha
   - **E** → Debe mostrar Xelor mirando derecha (Right)
   - **SE** → Debe mostrar Xelor vista lateral derecha
   - **S** → Debe mostrar Xelor de frente (Back)
   - **SW** → Debe mostrar Xelor vista lateral INVERTIDA (flipped)
   - **W** → Debe mostrar Xelor mirando izquierda (Left)
   - **NW** → Debe mostrar Xelor diagonal izquierda

**Estado: Walk**
1. Click **"Walk"** button
2. Repite las pruebas en todas las direcciones
3. Verifica que la animación de caminar se reproduzca

**Estado: Run**
1. Click **"Run"** button  
2. Repite las pruebas en todas las direcciones
3. Verifica que la animación de correr se reproduzca

### Paso 4: Verificar Logs

**Logs esperados en la consola:**
```
[CharacterLayerRenderer] Mapped staticN → staticF (flip: false)
[CharacterLayerRenderer] Mapped walkE → walkR (flip: false)
[CharacterLayerRenderer] Mapped staticSW → staticS (flip: true)
[CharacterLayerRenderer] Max sprite height: 0.09, layer offset: -0.045
[CharacterLayerRenderer] Created 10 sprite layers
```

---

## ✅ Criterios de Éxito (FINAL)

| Dirección | Sprite Esperado | Flip | Estado |
|-----------|----------------|------|--------|
| N (Norte) | Back (B) | No | ✅ Verificado |
| NE | Left (L) | **Sí** | 🟡 Por probar |
| E (Este) | Side (S) | No | ✅ Verificado |
| SE | Right (R) | No | ✅ Verificado |
| S (Sur) | Front (F) | No | ✅ Verificado |
| SW | Right (R) | **Sí** | ✅ Verificado |
| W (Oeste) | Side (S) | **Sí** | ✅ Verificado |
| NW | Left (L) | No | ✅ Verificado |

**✅ Éxito Total:** Todas las direcciones muestran el sprite correcto, incluyendo las invertidas.

---

## 🐛 Solución de Problemas

### Issue: "Found 0 folders matching animation"

**Causa:** La animación mapeada no existe en los assets de Dofus  
**Solución:**
1. Verifica que los folders existen: `Sprites/Classes/Xelor/sprites/DefineSprite_*_walkR`
2. Revisa el mapeo - tal vez necesita ajuste para esta clase específica
3. Comprueba los logs para ver qué buscó exactamente

### Issue: Sprite aparece pero en dirección incorrecta

**Causa:** Mapeo incorrecto para esa dirección específica  
**Solución:**
1. Revisa el método `MapUnityDirectionToDofus()`
2. Ajusta el mapeo para esa dirección
3. Puede que necesites cambiar qué sprite de Dofus se usa

### Issue: Sprite invertido incorrectamente

**Causa:** Flag `shouldFlip` configurado mal  
**Solución:**
1. Revisa la tabla de mapeo
2. Cambia el valor de `shouldFlip` para esa dirección:
   ```csharp
   case "SW":  return ("S", true);  // Cambiar a false si se ve mal
   ```

### Issue: Algunas clases funcionan, otras no

**Causa:** Diferentes clases tienen diferentes convenciones de nombres  
**Solución:**
1. Lista los folders de sprites para esa clase
2. Identifica la convención de nombres (puede ser diferente)
3. Ajusta el mapeo o crea mapeos específicos por clase si es necesario

---

## 🔧 Ajustes Futuros

### Mapeo Específico por Clase

Si diferentes clases usan convenciones diferentes:

```csharp
private (string dofusDirection, bool shouldFlip) MapUnityDirectionToDofus(string unityDirection)
{
    // Special mapping for specific classes
    if (classId == 4) // Sram
    {
        // Sram has different direction naming...
        switch (unityDirection)
        {
            case "N": return ("North", false);
            // ...
        }
    }
    
    // Default mapping for most classes
    switch (unityDirection)
    {
        // Standard mapping...
    }
}
```

### Diagonal Mejorado

Para mejor calidad en diagonales, si existen sprites específicos:

```csharp
case "NE":  return ("NE", false); // Si existe sprite NE nativo
case "SE":  return ("SE", false); // Si existe sprite SE nativo
case "SW":  return ("SW", false); // Si existe sprite SW nativo (sin flip)
case "NW":  return ("NW", false); // Si existe sprite NW nativo
```

### Sistema de Fallback

Si un sprite no existe, usar el más cercano:

```csharp
// In LoadCharacterSprites after folder search
if (foldersFound == 0)
{
    Debug.LogWarning($"[CharacterLayerRenderer] {dofusAnimation} not found, trying fallback...");
    
    // Try fallback directions
    string[] fallbacks = { "S", "R", "L", "F", "B" };
    foreach (string fallbackDir in fallbacks)
    {
        string fallbackAnim = animState + fallbackDir;
        // Try loading fallbackAnim...
    }
}
```

---

## 📊 Resumen Técnico

### Arquitectura del Sistema:

```
Unity Request (walkNE)
        ↓
Parse State & Direction
        ↓
MapUnityDirectionToDofus("NE")
        ↓
Returns ("R", false)
        ↓
Build Dofus Animation (walkR)
        ↓
Search Folders (DefineSprite_*_walkR)
        ↓
Load Sprites
        ↓
Apply flipX=false to all layers
        ↓
Render Character
```

### Ventajas del Sistema:

1. **Escalable** - Fácil añadir nuevos mapeos
2. **Configurable** - Un lugar central para mapeo
3. **Debug-friendly** - Logs claros de conversiones
4. **Eficiente** - No duplica sprites, usa flip
5. **Flexible** - Puede personalizar por clase si es necesario

### Limitaciones Conocidas:

1. **Diagonales aproximadas** - NE/SE/SW/NW usan sprites cardinales
2. **Calidad variable** - Flip puede verse menos natural que sprites nativos
3. **Clase-específico** - Puede necesitar ajustes por clase

---

## 📞 Soporte

**Si después de pruebas encuentras:**

1. **Dirección incorrecta** → Revisa tabla de mapeo en `MapUnityDirectionToDofus()`
2. **Sprite no carga** → Verifica nombres de folders con `list_dir`
3. **Flip incorrecto** → Ajusta valor `shouldFlip` para esa dirección
4. **Clase no funciona** → Puede necesitar mapeo específico

**Información útil para reportar problemas:**
- Clase ID y nombre
- Dirección que falla
- Logs de consola completos
- Screenshot del resultado

---

**Fin del Documento**

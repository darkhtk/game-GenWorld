# Phaser to Unity Mapping Reference

## Core Concepts

| Phaser 3 | Unity 2D | Notes |
|----------|----------|-------|
| Phaser.Scene | Unity Scene + MonoBehaviour | One Scene per game state |
| Phaser.GameObjects.Container | GameObject with children | Parent-child via Transform |
| Phaser.Physics.Arcade.Sprite | SpriteRenderer + Rigidbody2D + Collider2D | Add BoxCollider2D or CircleCollider2D |
| scene.add.text() | TextMeshProUGUI on Canvas | UI text only via TMP |
| scene.add.rectangle() | Image (UI) or SpriteRenderer (world) | |
| scene.tweens.add() | DOTween or Coroutine | DOTween preferred |
| scene.time.addEvent() | Invoke(), InvokeRepeating(), or Coroutine | |
| this.input.keyboard.addKey() | Input.GetKey(KeyCode.W) | Legacy input |
| this.input.on('pointerdown') | Input.GetMouseButtonDown(0) | |
| scene.cameras.main | Camera.main | Cache reference |
| camera.shake() | Cinemachine CinemachineImpulseSource | |
| Phaser.Math.Angle.Between() | Mathf.Atan2(dy, dx) | Returns radians |
| Phaser.Math.Distance.Between() | Vector2.Distance(a, b) | |
| setTintFill(color) | spriteRenderer.color = color | |
| setDepth(n) | spriteRenderer.sortingOrder = n | |
| BlendMode.ADD | Material with Additive shader | URP: Sprites/Default + Additive |
| GeometryMask | RectMask2D component | For scroll areas |
| setInteractive() + pointerover | IPointerEnterHandler or EventTrigger | UI only |
| localStorage | Application.persistentDataPath + File.IO | JSON file |
| fetch() / XMLHttpRequest | UnityWebRequest or HttpClient | For Ollama API |

## Coordinate System

Phaser: Y increases DOWN. Origin at top-left.
Unity: Y increases UP. Origin at center or bottom-left.

Conversion at boundaries:
```csharp
// Tile coords (from JSON) to Unity world position
float unityX = (tileX + 0.5f) * GameConfig.TileSize;
float unityY = -(tileY + 0.5f) * GameConfig.TileSize;  // NEGATE Y

// Unity world position to Tile coords
int tileX = Mathf.FloorToInt(worldX / GameConfig.TileSize);
int tileY = Mathf.FloorToInt(-worldY / GameConfig.TileSize);  // NEGATE Y
```

## Time Units

| Context | Phaser | Unity |
|---------|--------|-------|
| Cooldowns, durations in data | milliseconds (int) | Keep as ms in data, convert at use |
| Time.time | N/A | seconds (float) |
| Time.deltaTime | N/A | seconds (float) |
| Frame timing | scene.time.now (ms) | Time.time * 1000f (to ms) |

Pattern: Store and compare in ms. Convert only at Unity API boundaries.
```csharp
float nowMs = Time.time * 1000f;
if (nowMs >= _readyAtMs) { /* cooldown done */ }
```

## Physics

| Phaser Arcade | Unity 2D |
|---------------|----------|
| body.setVelocity(vx, vy) | rb.linearVelocity = new Vector2(vx, vy) |
| body.setBounce(0.5) | rb.sharedMaterial.bounciness = 0.5f |
| physics.overlap() | Physics2D.OverlapCircle() |
| physics.moveToObject() | Manual: rb.linearVelocity = dir * speed |
| Arcade collision | Collider2D + OnTriggerEnter2D |

## Sprite and Animation

| Phaser | Unity |
|--------|-------|
| this.anims.create() | Animator Controller + Animation Clips |
| sprite.play('walk_down') | animator.Play("walk_down") |
| Spritesheet grid slice | Sprite Editor, Slice by Grid (32x32) |
| Frame-based animation | Create Animation clip, drag frames |

## Saving

| Phaser | Unity |
|--------|-------|
| localStorage.setItem(key, json) | File.WriteAllText(path, json) |
| localStorage.getItem(key) | File.ReadAllText(path) |
| Save path | Browser storage | Application.persistentDataPath + "/rpg_save.json" |

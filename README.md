# Não É Mário 🍄 (M1 - Protótipo Grey Box)

Trabalho de Desenvolvimento de Jogos - Entrega 1 (Protótipo Funcional).

Esse é um joguinho 2D de plataforma side-scroller feito na Unity 6, no estilo Mario simplão — a ideia é só validar que TODAS as mecânicas pedidas no escopo do M1 funcionam (loop completo de jogo, score, vidas, HUD, sons, inimigos, etc). Ainda tá em "grey box" mesmo, todos os personagens são quadrados/retângulos coloridos. Os assets bonitos vêm nas próximas entregas.

## ✅ Checklist do M1

- [x] **Loop de Jogo**: Menu → Cena de Jogo → Game Over / Vitória
- [x] **Persistência de Score**: score na sessão + recorde salvo em disco (PlayerPrefs)
- [x] **Feedback ao Jogador**: HUD (score/recorde/moedas/vidas) + SFX (pulo, moeda, dano, stomp, vitória, click) + popup "+10" flutuante
- [x] **Escopo escolhido**: Side Scrolling (estilo Mario)

## 🎮 Como rodar (passo a passo)

> ⚠️ Precisa do **Unity 6000.4.3f1** instalado (qualquer versão que abra o projeto serve, mas eu testei nessa).

### 1. Abrir o projeto

1. Abra o **Unity Hub**.
2. Clique em **Add → Add project from disk**.
3. Selecione a pasta `não é mario` (essa pasta aqui).
4. Clique no projeto na lista pra abrir. (Primeira vez demora uns minutinhos pra importar.)

### 2. Abrir a cena

No painel **Project** (canto inferior esquerdo do editor), navegue até:

```
Assets → Scenes → SampleScene
```

Dê **duplo clique** em `SampleScene`.

### 3. Configurar a cena (única coisa manual!)

1. Na **Hierarchy** (painel à esquerda no topo), apague tudo se quiser deixar limpo (não é obrigatório, o script reaproveita o que já existir).
2. Clique com o **botão direito na Hierarchy** → **Create Empty**.
3. Renomeie esse novo objeto pra `Bootstrap` (ou qualquer nome, tanto faz).
4. Com ele selecionado, no **Inspector** (painel à direita) clique em **Add Component**.
5. Digite `Game Bootstrap` e clique no script que aparecer.
6. Aperte `Ctrl + S` pra salvar a cena.

### 4. Apertar Play

Clique no botão ▶ lá em cima no centro do editor. O menu vai aparecer e é só clicar em **JOGAR**.

## 🎮 Controles

| Ação | Teclas |
|---|---|
| Mover | `A` `D` ou setas `←` `→` |
| Pular | `Espaço`, `W` ou seta `↑` |

## 📊 Sistema de pontuação

| Ação | Pontos |
|---|---|
| Pegar moeda 🪙 | +10 |
| Pisar em inimigo 👾 | +50 |
| Bônus de vitória 🏁 | +100 por vida restante |

O recorde fica salvo entre sessões (não some quando fecha o jogo).

## 🧠 Como funciona (arquitetura)

Decidi montar tudo por código, sem ficar arrastando coisa no editor. O único componente que precisa ser adicionado manualmente é o `GameBootstrap` — ele constrói o resto da cena em runtime (player, plataformas, moedas, inimigos, UI, câmera, sons).

Estrutura dos scripts (em [Assets/Scripts/](Assets/Scripts/)):

| Arquivo | O que faz |
|---|---|
| [GameBootstrap.cs](Assets/Scripts/GameBootstrap.cs) | Monta a cena inteira. **É o ponto de entrada.** |
| [GameManager.cs](Assets/Scripts/GameManager.cs) | Singleton. Score, vidas, recorde, estado do jogo. |
| [SfxPlayer.cs](Assets/Scripts/SfxPlayer.cs) | Toca os SFX (gerados por código com onda senoidal). |
| [PlayerController2D.cs](Assets/Scripts/PlayerController2D.cs) | Movimento e pulo do player. |
| [EnemyPatrol.cs](Assets/Scripts/EnemyPatrol.cs) | IA do inimigo (patrulha + leva stomp). |
| [Coin.cs](Assets/Scripts/Coin.cs) | Moeda coletável. |
| [Goal.cs](Assets/Scripts/Goal.cs) | Bandeira de fim de fase. |
| [KillZone.cs](Assets/Scripts/KillZone.cs) | Zona de morte (cair fora do mapa). |
| [CameraFollow2D.cs](Assets/Scripts/CameraFollow2D.cs) | Câmera lateral suave. |
| [UIController.cs](Assets/Scripts/UIController.cs) | Constrói Menu, HUD, Game Over e Vitória por código. |
| [ScorePopup.cs](Assets/Scripts/ScorePopup.cs) | Aquele "+10" amarelo que sobe e some. |

Padrões de projeto que usei:

- **Singleton** no `GameManager` e `SfxPlayer` (pra ter acesso global fácil).
- **Observer** com `event System.Action` pra UI atualizar quando score/vidas/estado mudam (assim não preciso fazer polling no Update).
- **State Machine** simples no `GameManager` (Menu → Playing → GameOver/Victory).

## 🐛 Se der ruim

| Problema | Solução |
|---|---|
| Tela toda azul, nada acontece ao apertar Play | Você esqueceu de adicionar o componente `GameBootstrap` na cena |
| Erro `InputSystem` not found | Vai em **Window → Package Manager** e confirma que `Input System` ta instalado |
| Nada de som | Pode ser o sistema do PC mesmo. No editor, confirma se o ícone de som no Game View não tá mutado |
| Botões do menu não respondem ao clique | Talvez tem dois `EventSystem` na cena. Apaga um |
| `Both Input Systems disabled` | **Edit → Project Settings → Player → Active Input Handling → Both** e reinicia o editor |

## 🚧 O que falta pras próximas entregas (M2/M3)

- [ ] Trocar os blocos coloridos por sprites de verdade
- [ ] Música de fundo + SFX de verdade (não esses beeps gerados rs)
- [ ] Animações (andar, pular, morrer)
- [ ] Mais fases / dificuldade progressiva
- [ ] Power-ups
- [ ] Tela de pause

## 📝 Créditos

- Engine: **Unity 6** (com Universal Render Pipeline)
- Input: **Unity New Input System**
- Tudo que tem aqui dentro foi feito do zero, sem assets externos.
- Sons gerados em runtime com onda senoidal (matemática que vimos em sinais 😄).

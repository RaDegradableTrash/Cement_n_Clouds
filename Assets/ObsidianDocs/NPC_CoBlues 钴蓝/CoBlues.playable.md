# 钴蓝 · 可编辑剧情

在 Obsidian 中修改 `> ` 后的台词。每个节点与选项下方的元数据保留条件和分支；可修改 next 指向另一个节点 id。保存后回到 Unity 自动检查并导入，错误会保留原来的有效内容。

```dialogue-content
{
    "version": 1,
    "npcId": "npc.cobalt",
    "speaker": "钴蓝",
    "entry": "first",
    "nodes": []
}
```

## first

```dialogue-node
{
    "id": "first",
    "text": "",
    "fallback": "topics",
    "effect": "met",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "greet.return",
    "observation": "",
    "textVariants": [
        "纸在脚边，小心点。看画可以站这里。",
        "往这边站吧，别踩着画纸。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 留神脚边的画纸。想看就站这边。

### 回应 · first.silent

```dialogue-choice
{
    "id": "first.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · first.money

```dialogue-choice
{
    "id": "first.money",
    "text": "",
    "next": "money",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这些画不拿去卖吗？

### 回应 · first.watch

```dialogue-choice
{
    "id": "first.watch",
    "text": "",
    "next": "watch",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我就看看，不碰。

### 回应 · first.leave

```dialogue-choice
{
    "id": "first.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，我绕开走。

## money

```dialogue-node
{
    "id": "money",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.sale",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "return.sale",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 暂时不卖。……钱真有那么重要吗？

### 回应 · money.silent

```dialogue-choice
{
    "id": "money.silent",
    "text": "",
    "next": "silence.sale",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · money.use

```dialogue-choice
{
    "id": "money.use",
    "text": "",
    "next": "money.useful",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 买颜料的时候应该挺重要。

### 回应 · money.shy

```dialogue-choice
{
    "id": "money.shy",
    "text": "",
    "next": "money.shy",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你是不想卖，还是不想去镇上？

### 回应 · money.stop

```dialogue-choice
{
    "id": "money.stop",
    "text": "",
    "next": "sale.respect",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 也是，你想怎么画就怎么画。

## money.useful

```dialogue-node
{
    "id": "money.useful",
    "text": "",
    "fallback": "money.supplied",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "needs_paint",
            "value": "true"
        }
    ],
    "choices": []
}
```

> 蓝色快空了，得买。鱼倒能自己钓。

### 回应 · money.useful.silent

```dialogue-choice
{
    "id": "money.useful.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · use.paint

```dialogue-choice
{
    "id": "use.paint",
    "text": "",
    "next": "paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 缺的是哪种蓝？

### 回应 · use.fish

```dialogue-choice
{
    "id": "use.fish",
    "text": "",
    "next": "fish",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你吃的鱼都是自己钓的？

### 回应 · use.leave

```dialogue-choice
{
    "id": "use.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那不耽误你画了。

## money.shy

```dialogue-node
{
    "id": "money.shy",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.sale",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "return.sale",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 不太想去镇上。去了就得给画定价。

### 回应 · money.shy.silent

```dialogue-choice
{
    "id": "money.shy.silent",
    "text": "",
    "next": "silence.sale",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · shy.watch

```dialogue-choice
{
    "id": "shy.watch",
    "text": "",
    "next": "watch",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那我先看看画。

### 回应 · shy.painting

```dialogue-choice
{
    "id": "shy.painting",
    "text": "",
    "next": "painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你最想画下来的是什么？

### 回应 · shy.sell

```dialogue-choice
{
    "id": "shy.sell",
    "text": "",
    "next": "sell",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 让别人看看，也不一定要卖出去。

## watch

```dialogue-node
{
    "id": "watch",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [
        "中间这片水，我每天都得重新看。",
        "看这片水。隔一天，颜色就不一样。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 看中间那片水。每天颜色都不同。

### 回应 · watch.silent

```dialogue-choice
{
    "id": "watch.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · watch.painting

```dialogue-choice
{
    "id": "watch.painting",
    "text": "",
    "next": "painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这片水里，你最想画哪儿？

### 回应 · watch.leave

```dialogue-choice
{
    "id": "watch.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我看见了，你接着画。

## topics

```dialogue-node
{
    "id": "topics",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.neutral",
    "topic": "neutral",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，你说。

### 回应 · topics.silent

```dialogue-choice
{
    "id": "topics.silent",
    "text": "",
    "next": "silence.neutral",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · topics.paint

```dialogue-choice
{
    "id": "topics.paint",
    "text": "",
    "next": "paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 蓝色颜料还够用吗？

### 回应 · topics.camp

```dialogue-choice
{
    "id": "topics.camp",
    "text": "",
    "next": "tent",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 营地这边需要搭把手吗？

### 回应 · topics.daily

```dialogue-choice
{
    "id": "topics.daily",
    "text": "",
    "next": "daily",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你刚才在本子上记什么？

### 回应 · topics.leave

```dialogue-choice
{
    "id": "topics.leave",
    "text": "",
    "next": "bye.neutral",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 没别的事，我先走了。

## painting

```dialogue-node
{
    "id": "painting",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [
        "水挨着石头那块。光一变，它就变。",
        "我盯的是水边。想留住它变光的样子。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 水贴着石头那块。我想画它变光的样子。

### 回应 · painting.silent

```dialogue-choice
{
    "id": "painting.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · painting.joke

```dialogue-choice
{
    "id": "painting.joke",
    "text": "",
    "next": "painting.joke",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 为什么不在一张里画完？

### 回应 · painting.paint

```dialogue-choice
{
    "id": "painting.paint",
    "text": "",
    "next": "paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你用的是哪种蓝？

### 回应 · painting.leave

```dialogue-choice
{
    "id": "painting.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那你先画眼前这一张。

## painting.joke

```dialogue-node
{
    "id": "painting.joke",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 光不同，石头也不同。得分成两张画。

### 回应 · painting.joke.silent

```dialogue-choice
{
    "id": "painting.joke.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · joke.topics

```dialogue-choice
{
    "id": "joke.topics",
    "text": "",
    "next": "painting.moments",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 明白了，你是在画不同的时刻。

### 回应 · joke.leave

```dialogue-choice
{
    "id": "joke.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我下次再看看另一张。

## fish

```dialogue-node
{
    "id": "fish",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.fish",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "greet.fish",
    "observation": "",
    "textVariants": [
        "多半自己钓。够一顿，就回来画画。",
        "嗯，钓够吃的就收。还得回来画画。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 大部分是。够吃就收，留点时间画画。

### 回应 · fish.silent

```dialogue-choice
{
    "id": "fish.silent",
    "text": "",
    "next": "silence.fish",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · fish.daily

```dialogue-choice
{
    "id": "fish.daily",
    "text": "",
    "next": "fish.catch",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 要是没钓够呢？

### 回应 · fish.leave

```dialogue-choice
{
    "id": "fish.leave",
    "text": "",
    "next": "fish.enough",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这样还能留出时间画画。

## daily

```dialogue-node
{
    "id": "daily",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.notebook",
    "topic": "notebook",
    "leaveNext": "bye.notebook",
    "returnEntry": "greet.notebook",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 今天的潮水。那块石头比昨天淹得早。

### 回应 · daily.silent

```dialogue-choice
{
    "id": "daily.silent",
    "text": "",
    "next": "silence.notebook",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · daily.sell

```dialogue-choice
{
    "id": "daily.sell",
    "text": "",
    "next": "notebook.notes",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 除了潮水，还记些什么？

### 回应 · daily.painting

```dialogue-choice
{
    "id": "daily.painting",
    "text": "",
    "next": "painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这片水里，你最想画哪儿？

### 回应 · daily.leave

```dialogue-choice
{
    "id": "daily.leave",
    "text": "",
    "next": "bye.notebook",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那你趁还记得清，先写完。

## sell

```dialogue-node
{
    "id": "sell",
    "text": "",
    "fallback": "sale.private",
    "effect": "",
    "silenceNext": "silence.sale",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "return.sale",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "familiar",
            "id": "",
            "value": ""
        }
    ],
    "choices": []
}
```

> 我怕摆一整天也没人买。……有点丢脸。

### 回应 · sell.silent

```dialogue-choice
{
    "id": "sell.silent",
    "text": "",
    "next": "silence.sale",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · sell.try

```dialogue-choice
{
    "id": "sell.try",
    "text": "",
    "next": "sell.consider",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 先试一幅小的，卖不掉就带回来。

### 回应 · sell.stop

```dialogue-choice
{
    "id": "sell.stop",
    "text": "",
    "next": "sale.respect",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那先不试。等你自己想去。

## sell.consider

```dialogue-node
{
    "id": "sell.consider",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.sale",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "return.sale",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 也行。先挑一幅小的，等这张画完再说。

### 回应 · sell.consider.silent

```dialogue-choice
{
    "id": "sell.consider.silent",
    "text": "",
    "next": "silence.sale",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · consider.leave

```dialogue-choice
{
    "id": "consider.leave",
    "text": "",
    "next": "sale.respect",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，什么时候去你自己定。

### 回应 · consider.topics

```dialogue-choice
{
    "id": "consider.topics",
    "text": "",
    "next": "sale.keep",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你最舍不得拿走的是哪幅？

## paint

```dialogue-node
{
    "id": "paint",
    "text": "",
    "fallback": "paint.resolved",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "needs_paint",
            "value": "true"
        }
    ],
    "choices": []
}
```

> 钴蓝快用完了。水和阴影都得用它。

### 回应 · paint.silent

```dialogue-choice
{
    "id": "paint.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · paint.more

```dialogue-choice
{
    "id": "paint.more",
    "text": "",
    "next": "painting",
    "speechAct": "ask",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你主要在画海的哪一块？

### 回应 · paint.shop

```dialogue-choice
{
    "id": "paint.shop",
    "text": "",
    "next": "paint.shop",
    "speechAct": "ask",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你平时去哪买？

### 回应 · paint.offer

```dialogue-choice
{
    "id": "paint.offer",
    "text": "",
    "next": "paint.offer",
    "speechAct": "offer",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我可以帮你留意。

## paint.shop

```dialogue-node
{
    "id": "paint.shop",
    "text": "",
    "fallback": "paint.resolved",
    "effect": "know_shop",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 镇上的画材店。一管钴蓝就够了。

### 回应 · paint.shop.silent

```dialogue-choice
{
    "id": "paint.shop.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · shop.offer

```dialogue-choice
{
    "id": "shop.offer",
    "text": "",
    "next": "paint.offer",
    "speechAct": "offer",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我路过时帮你看看有没有。

### 回应 · shop.promise

```dialogue-choice
{
    "id": "shop.promise",
    "text": "",
    "next": "paint.accept",
    "speechAct": "promise",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": [
        {
            "op": "fact",
            "id": "needs_paint",
            "value": "true"
        },
        {
            "op": "promise_absent",
            "id": "",
            "value": ""
        }
    ]
}
```

> 我明天给你带一管蓝色颜料。

### 回应 · shop.leave

```dialogue-choice
{
    "id": "shop.leave",
    "text": "",
    "next": "bye.paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 知道是哪家了。我先走了。

## paint.offer

```dialogue-node
{
    "id": "paint.offer",
    "text": "",
    "fallback": "paint.resolved",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "needs_paint",
            "value": "true"
        }
    ],
    "choices": []
}
```

> 好，顺路看一眼就行。别特地跑。

### 回应 · paint.offer.silent

```dialogue-choice
{
    "id": "paint.offer.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · offer.promise

```dialogue-choice
{
    "id": "offer.promise",
    "text": "",
    "next": "paint.accept",
    "speechAct": "promise",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": [
        {
            "op": "fact",
            "id": "needs_paint",
            "value": "true"
        },
        {
            "op": "promise_absent",
            "id": "",
            "value": ""
        }
    ]
}
```

> 我明天给你带一管蓝色颜料。

### 回应 · offer.shop

```dialogue-choice
{
    "id": "offer.shop",
    "text": "",
    "next": "paint.shop.confirm",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": [
        {
            "op": "knows",
            "id": "paint_shop",
            "value": ""
        }
    ]
}
```

> 就你说的那家画材店，对吧？

### 回应 · offer.leave

```dialogue-choice
{
    "id": "offer.leave",
    "text": "",
    "next": "paint.noappointment",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那就碰到了再说，不先约时间。

## paint.accept

```dialogue-node
{
    "id": "paint.accept",
    "text": "",
    "fallback": "paint.resolved",
    "effect": "accept_paint",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.promise",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "needs_paint",
            "value": "true"
        }
    ],
    "choices": []
}
```

> 好，明天带一管钴蓝。买不到就告诉我。

### 回应 · paint.accept.silent

```dialogue-choice
{
    "id": "paint.accept.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · accept.leave

```dialogue-choice
{
    "id": "accept.leave",
    "text": "",
    "next": "bye.promise",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 嗯，明天见。

### 回应 · accept.release

```dialogue-choice
{
    "id": "accept.release",
    "text": "",
    "next": "paint.release",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我不一定赶得上，先别约了。

### 回应 · accept.topics

```dialogue-choice
{
    "id": "accept.topics",
    "text": "",
    "next": "painting.detail",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 准备先补哪里？

## paint.release

```dialogue-node
{
    "id": "paint.release",
    "text": "",
    "fallback": "topics",
    "effect": "release_paint",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 好，那就取消。我自己去买。

### 回应 · paint.release.silent

```dialogue-choice
{
    "id": "paint.release.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · release.leave

```dialogue-choice
{
    "id": "release.leave",
    "text": "",
    "next": "bye.paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，不让你白等。

### 回应 · release.topics

```dialogue-choice
{
    "id": "release.topics",
    "text": "",
    "next": "painting.stone",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 缺蓝色的时候，你先画哪部分？

## paint.resolved

```dialogue-node
{
    "id": "paint.resolved",
    "text": "",
    "fallback": "paint",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "needs_paint",
            "value": "false"
        }
    ],
    "choices": []
}
```

> 用的是钴蓝。现在够了，不用替我找。

### 回应 · paint.resolved.silent

```dialogue-choice
{
    "id": "paint.resolved.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · resolved.painting

```dialogue-choice
{
    "id": "resolved.painting",
    "text": "",
    "next": "painting.detail",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那先补哪里？

### 回应 · resolved.leave

```dialogue-choice
{
    "id": "resolved.leave",
    "text": "",
    "next": "bye.paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，不用我找了。

## paint.thanks

```dialogue-node
{
    "id": "paint.thanks",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "memory",
            "id": "gave_blue_paint",
            "value": ""
        }
    ],
    "choices": []
}
```

> 收到了，谢谢。正缺这个蓝。

### 回应 · paint.thanks.silent

```dialogue-choice
{
    "id": "paint.thanks.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · thanks.small

```dialogue-choice
{
    "id": "thanks.small",
    "text": "",
    "next": "paint.received",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 正好有一管，你用着吧。

### 回应 · thanks.leave

```dialogue-choice
{
    "id": "thanks.leave",
    "text": "",
    "next": "bye.paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那你先用，我去走走。

## paint.recall

```dialogue-node
{
    "id": "paint.recall",
    "text": "",
    "fallback": "topics",
    "effect": "recall_paint",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "memory",
            "id": "gave_blue_paint",
            "value": ""
        }
    ],
    "choices": []
}
```

> 你送的蓝，用在水边阴影里了。

### 回应 · paint.recall.silent

```dialogue-choice
{
    "id": "paint.recall.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · recall.watch

```dialogue-choice
{
    "id": "recall.watch",
    "text": "",
    "next": "paint.recall.detail",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 哪一块？

### 回应 · recall.topics

```dialogue-choice
{
    "id": "recall.topics",
    "text": "",
    "next": "painting.shadow",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 为什么阴影里也要放蓝色？

### 回应 · recall.leave

```dialogue-choice
{
    "id": "recall.leave",
    "text": "",
    "next": "bye.neutral",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这次先不看了，下回吧。

## tent

```dialogue-node
{
    "id": "tent",
    "text": "",
    "fallback": "tent.done",
    "effect": "",
    "silenceNext": "silence.tent",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "return.tent",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "tent_done",
            "value": "false"
        }
    ],
    "choices": []
}
```

> 帮我压一下这块布？我腾不开手绑绳子。

### 回应 · tent.silent

```dialogue-choice
{
    "id": "tent.silent",
    "text": "",
    "next": "silence.tent",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · tent.yes

```dialogue-choice
{
    "id": "tent.yes",
    "text": "",
    "next": "tent.wait",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，等我过去。

### 回应 · tent.joke

```dialogue-choice
{
    "id": "tent.joke",
    "text": "",
    "next": "tent.joke",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 是这个结总松吗？

### 回应 · tent.no

```dialogue-choice
{
    "id": "tent.no",
    "text": "",
    "next": "tent.decline",
    "speechAct": "decline",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我现在腾不出手。

## tent.wait

```dialogue-node
{
    "id": "tent.wait",
    "text": "",
    "fallback": "tent.done",
    "effect": "",
    "silenceNext": "silence.tent",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "return.tent",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "tent_done",
            "value": "false"
        }
    ],
    "choices": []
}
```

> 脚边这块。压住布，别踩绳子。

### 回应 · tent.wait.silent

```dialogue-choice
{
    "id": "tent.wait.silent",
    "text": "",
    "next": "silence.tent",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · wait.topics

```dialogue-choice
{
    "id": "wait.topics",
    "text": "",
    "next": "tent.joke",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 换个结会牢一点吗？

### 回应 · wait.leave

```dialogue-choice
{
    "id": "wait.leave",
    "text": "",
    "next": "tent.focus",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我先不说话，你绑吧。

## tent.joke

```dialogue-node
{
    "id": "tent.joke",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.tent",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "return.tent",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，绳头太短容易滑。这次多留一截。

### 回应 · tent.joke.silent

```dialogue-choice
{
    "id": "tent.joke.silent",
    "text": "",
    "next": "silence.tent",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · tentjoke.yes

```dialogue-choice
{
    "id": "tentjoke.yes",
    "text": "",
    "next": "tent.wait",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 明白了，我来压布。

### 回应 · tentjoke.no

```dialogue-choice
{
    "id": "tentjoke.no",
    "text": "",
    "next": "tent.decline",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 帮不上了，小心手。

## tent.decline

```dialogue-node
{
    "id": "tent.decline",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.tent",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "return.tent",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 没事，我拿石头压着。

### 回应 · tent.decline.silent

```dialogue-choice
{
    "id": "tent.decline.silent",
    "text": "",
    "next": "silence.tent",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · decline.topics

```dialogue-choice
{
    "id": "decline.topics",
    "text": "",
    "next": "tent.stone",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 石头能压稳吗？

### 回应 · decline.leave

```dialogue-choice
{
    "id": "decline.leave",
    "text": "",
    "next": "bye.tent",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，那我先走。

## tent.done

```dialogue-node
{
    "id": "tent.done",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.tent",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "return.tent",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "tent_done",
            "value": "true"
        }
    ],
    "choices": []
}
```

> 这块已经绑好了，谢谢。

### 回应 · tent.done.silent

```dialogue-choice
{
    "id": "tent.done.silent",
    "text": "",
    "next": "silence.tent",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · done.topics

```dialogue-choice
{
    "id": "done.topics",
    "text": "",
    "next": "tent.trust",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这回看着稳多了。

### 回应 · done.leave

```dialogue-choice
{
    "id": "done.leave",
    "text": "",
    "next": "bye.tent",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那就好，我先走。

## greet.return

```dialogue-node
{
    "id": "greet.return",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [
        "回来了？水边这里还没改完。",
        "又见着了。这一小块还在磨。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 又来了？水边这一块还没画完。

### 回应 · greet.return.silent

```dialogue-choice
{
    "id": "greet.return.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · return.blue

```dialogue-choice
{
    "id": "return.blue",
    "text": "",
    "next": "painting.detail",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你指的是哪一块？

### 回应 · return.stay

```dialogue-choice
{
    "id": "return.stay",
    "text": "",
    "next": "watch.company",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 嗯，我再看一会儿。

### 回应 · return.leave

```dialogue-choice
{
    "id": "return.leave",
    "text": "",
    "next": "bye.neutral",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我只是经过，先不看了。

## greet.notebook

```dialogue-node
{
    "id": "greet.notebook",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.notebook",
    "topic": "notebook",
    "leaveNext": "bye.notebook",
    "returnEntry": "greet.notebook",
    "observation": "",
    "textVariants": [
        "记几笔潮水，不然过会儿记不准了。",
        "先把潮水记上。拖一会儿又忘了。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 在记潮水。不趁现在记，转头就忘。

### 回应 · greet.notebook.silent

```dialogue-choice
{
    "id": "greet.notebook.silent",
    "text": "",
    "next": "silence.notebook",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · notebook.ask

```dialogue-choice
{
    "id": "notebook.ask",
    "text": "",
    "next": "notebook.notes",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这个本子只记潮水？

### 回应 · notebook.tease

```dialogue-choice
{
    "id": "notebook.tease",
    "text": "",
    "next": "notebook.mess",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 每天都记，不麻烦吗？

### 回应 · notebook.leave

```dialogue-choice
{
    "id": "notebook.leave",
    "text": "",
    "next": "bye.notebook",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那你先记完。

## greet.water

```dialogue-node
{
    "id": "greet.water",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [
        "太阳一出来，水边那点灰就透蓝了。",
        "看水边。刚才灰着，现在又蓝了。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 水边刚才还灰着。出太阳就蓝了。

### 回应 · greet.water.silent

```dialogue-choice
{
    "id": "greet.water.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · water.wait

```dialogue-choice
{
    "id": "water.wait",
    "text": "",
    "next": "painting.wait",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 等它变回灰色，再接着画？

### 回应 · water.paint

```dialogue-choice
{
    "id": "water.paint",
    "text": "",
    "next": "painting.unfinished",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 光总在变，画到一半怎么办？

### 回应 · water.leave

```dialogue-choice
{
    "id": "water.leave",
    "text": "",
    "next": "bye.neutral",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我去另一边看看。

## greet.fish

```dialogue-node
{
    "id": "greet.fish",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.fish",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "greet.fish",
    "observation": "",
    "textVariants": [
        "够一顿就收竿。不然一天就过去了。",
        "我一般钓够吃的就回来，不守一整天。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 我钓够一顿就收。不想一天都耗着。

### 回应 · greet.fish.silent

```dialogue-choice
{
    "id": "greet.fish.silent",
    "text": "",
    "next": "silence.fish",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · fish.smell

```dialogue-choice
{
    "id": "fish.smell",
    "text": "",
    "next": "fish.paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 想把时间留给画画？

### 回应 · fish.catch

```dialogue-choice
{
    "id": "fish.catch",
    "text": "",
    "next": "fish.catch",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 没钓够的时候怎么办？

### 回应 · fish.pass

```dialogue-choice
{
    "id": "fish.pass",
    "text": "",
    "next": "bye.neutral",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 明白，你先忙。

## greet.morning

```dialogue-node
{
    "id": "greet.morning",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.morning",
    "topic": "morning",
    "leaveNext": "bye.morning",
    "returnEntry": "greet.morning",
    "observation": "",
    "textVariants": [
        "早，趁这会儿记两笔颜色。",
        "来得早啊。我正记水边这点颜色。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 早。我先记一下水边的颜色。

### 回应 · greet.morning.silent

```dialogue-choice
{
    "id": "greet.morning.silent",
    "text": "",
    "next": "silence.morning",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · morning.water

```dialogue-choice
{
    "id": "morning.water",
    "text": "",
    "next": "morning.water",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这么早，吃过东西了吗？

### 回应 · morning.work

```dialogue-choice
{
    "id": "morning.work",
    "text": "",
    "next": "morning.light",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 已经画了一会儿了？

### 回应 · morning.leave

```dialogue-choice
{
    "id": "morning.leave",
    "text": "",
    "next": "bye.morning",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那我先不打扰你。

## greet.breakfast

```dialogue-node
{
    "id": "greet.breakfast",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.meal",
    "topic": "morning",
    "leaveNext": "bye.meal",
    "returnEntry": "greet.morning",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 早，正要烧水。

### 回应 · greet.breakfast.silent

```dialogue-choice
{
    "id": "greet.breakfast.silent",
    "text": "",
    "next": "silence.meal",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · breakfast.big

```dialogue-choice
{
    "id": "breakfast.big",
    "text": "",
    "next": "morning.water",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 早饭还没吃？

### 回应 · breakfast.watch

```dialogue-choice
{
    "id": "breakfast.watch",
    "text": "",
    "next": "watch.company",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你先烧水，我看看画。

### 回应 · breakfast.leave

```dialogue-choice
{
    "id": "breakfast.leave",
    "text": "",
    "next": "bye.meal",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，你先忙。

## greet.night

```dialogue-node
{
    "id": "greet.night",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.night",
    "topic": "night",
    "leaveNext": "bye.night",
    "returnEntry": "greet.night",
    "observation": "",
    "textVariants": [
        "再看一眼。天暗了，也该收画了。",
        "这会儿看不清了，我准备把画收起来。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 天暗了。再看一眼就收画。

### 回应 · greet.night.silent

```dialogue-choice
{
    "id": "greet.night.silent",
    "text": "",
    "next": "silence.night",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · night.work

```dialogue-choice
{
    "id": "night.work",
    "text": "",
    "next": "night.colour",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这么晚还看得清颜色？

### 回应 · night.sleep

```dialogue-choice
{
    "id": "night.sleep",
    "text": "",
    "next": "night.sleep",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你不困吗？

### 回应 · night.leave

```dialogue-choice
{
    "id": "night.leave",
    "text": "",
    "next": "bye.night",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那你早点休息。

## greet.lamp

```dialogue-node
{
    "id": "greet.lamp",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.night",
    "topic": "night",
    "leaveNext": "bye.night",
    "returnEntry": "greet.night",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 灯下看不准颜色，只能看看明暗。

### 回应 · greet.lamp.silent

```dialogue-choice
{
    "id": "greet.lamp.silent",
    "text": "",
    "next": "silence.night",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · lamp.dark

```dialogue-choice
{
    "id": "lamp.dark",
    "text": "",
    "next": "night.colour",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 颜色不准，还能看出什么？

### 回应 · lamp.stay

```dialogue-choice
{
    "id": "lamp.stay",
    "text": "",
    "next": "night.look",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我也看看哪块太亮。

### 回应 · lamp.leave

```dialogue-choice
{
    "id": "lamp.leave",
    "text": "",
    "next": "bye.night",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你看完早点休息。

## greet.paint

```dialogue-node
{
    "id": "greet.paint",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 你也带着颜料？我这管蓝快空了。

### 回应 · greet.paint.silent

```dialogue-choice
{
    "id": "greet.paint.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · carrying.tell

```dialogue-choice
{
    "id": "carrying.tell",
    "text": "",
    "next": "paint.ready",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我带了一管钴蓝，等会递给你。

### 回应 · carrying.look

```dialogue-choice
{
    "id": "carrying.look",
    "text": "",
    "next": "painting.detail",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你准备用来补画里的哪块？

### 回应 · carrying.leave

```dialogue-choice
{
    "id": "carrying.leave",
    "text": "",
    "next": "bye.neutral",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我先整理一下东西。

## paint.ready

```dialogue-node
{
    "id": "paint.ready",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 正好。递给我就行。

### 回应 · paint.ready.silent

```dialogue-choice
{
    "id": "paint.ready.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · ready.colour

```dialogue-choice
{
    "id": "ready.colour",
    "text": "",
    "next": "painting.shadow",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 阴影里也要放蓝色？

### 回应 · ready.leave

```dialogue-choice
{
    "id": "ready.leave",
    "text": "",
    "next": "bye.paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，先不聊了。

## greet.promise

```dialogue-node
{
    "id": "greet.promise",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.promise",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 约好的钴蓝还记得吧？有变化告诉我。

### 回应 · greet.promise.silent

```dialogue-choice
{
    "id": "greet.promise.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · promise.remember

```dialogue-choice
{
    "id": "promise.remember",
    "text": "",
    "next": "promise.unhurried",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 记着，还是按约好的时间。

### 回应 · promise.cancel

```dialogue-choice
{
    "id": "promise.cancel",
    "text": "",
    "next": "paint.release",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我赶不上了，先取消吧。

### 回应 · promise.other

```dialogue-choice
{
    "id": "promise.other",
    "text": "",
    "next": "painting.detail",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你等着补的是画里的哪块？

## promise.unhurried

```dialogue-node
{
    "id": "promise.unhurried",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.promise",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 好，照原来约的。我先画别的。

### 回应 · promise.unhurried.silent

```dialogue-choice
{
    "id": "promise.unhurried.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · unhurried.watch

```dialogue-choice
{
    "id": "unhurried.watch",
    "text": "",
    "next": "painting.stone",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 哪部分不需要蓝色？

### 回应 · unhurried.leave

```dialogue-choice
{
    "id": "unhurried.leave",
    "text": "",
    "next": "bye.promise",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 行，我先去办自己的事。

## greet.overdue

```dialogue-node
{
    "id": "greet.overdue",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 约好的时间过了。颜料那边怎么样？

### 回应 · greet.overdue.silent

```dialogue-choice
{
    "id": "greet.overdue.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · overdue.sorry

```dialogue-choice
{
    "id": "overdue.sorry",
    "text": "",
    "next": "promise.late",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 是我耽搁了，还没带来。

### 回应 · overdue.release

```dialogue-choice
{
    "id": "overdue.release",
    "text": "",
    "next": "paint.release",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我带不来了，取消吧。

### 回应 · overdue.paint

```dialogue-choice
{
    "id": "overdue.paint",
    "text": "",
    "next": "paint.waiting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 缺这管蓝，耽误画画了吗？

## promise.late

```dialogue-node
{
    "id": "promise.late",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，已经迟了。确定带不了就告诉我。

### 回应 · promise.late.silent

```dialogue-choice
{
    "id": "promise.late.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · late.release

```dialogue-choice
{
    "id": "late.release",
    "text": "",
    "next": "paint.release",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那取消吧，别再等我。

### 回应 · late.stay

```dialogue-choice
{
    "id": "late.stay",
    "text": "",
    "next": "watch.company",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我先看看画，可以吗？

## greet.tent

```dialogue-node
{
    "id": "greet.tent",
    "text": "",
    "fallback": "return.tent",
    "effect": "",
    "silenceNext": "silence.tent",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "return.tent",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "tent_done",
            "value": "true"
        }
    ],
    "choices": []
}
```

> 你帮我压过的那块布，结还没松。

### 回应 · greet.tent.silent

```dialogue-choice
{
    "id": "greet.tent.silent",
    "text": "",
    "next": "silence.tent",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · tentreturn.hold

```dialogue-choice
{
    "id": "tentreturn.hold",
    "text": "",
    "next": "tent.trust",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你这回把绳头留长了？

### 回应 · tentreturn.paint

```dialogue-choice
{
    "id": "tentreturn.paint",
    "text": "",
    "next": "tent.finished",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 终于能先放下这件事了。

### 回应 · tentreturn.leave

```dialogue-choice
{
    "id": "tentreturn.leave",
    "text": "",
    "next": "bye.tent",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 没松就好，我先走了。

## tent.trust

```dialogue-node
{
    "id": "tent.trust",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.tent",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "return.tent",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，多留了点绳头。这回拉不松。

### 回应 · tent.trust.silent

```dialogue-choice
{
    "id": "tent.trust.silent",
    "text": "",
    "next": "silence.tent",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · trust.joke

```dialogue-choice
{
    "id": "trust.joke",
    "text": "",
    "next": "tent.travel",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 先别把压布的石头挪开。

### 回应 · trust.leave

```dialogue-choice
{
    "id": "trust.leave",
    "text": "",
    "next": "bye.tent",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，起风再检查一下。

## tent.travel

```dialogue-node
{
    "id": "tent.travel",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.tent",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "return.tent",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 对，石头先留着。松了还能顶一下。

### 回应 · tent.travel.silent

```dialogue-choice
{
    "id": "tent.travel.silent",
    "text": "",
    "next": "silence.tent",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · travel.fish

```dialogue-choice
{
    "id": "travel.fish",
    "text": "",
    "next": "tent.finished",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那现在可以歇一会儿了。

### 回应 · travel.leave

```dialogue-choice
{
    "id": "travel.leave",
    "text": "",
    "next": "bye.tent",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 行，我不挪它。

## notebook.notes

```dialogue-node
{
    "id": "notebook.notes",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.notebook",
    "topic": "notebook",
    "leaveNext": "bye.notebook",
    "returnEntry": "greet.notebook",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 鱼在哪儿钓的，石头什么时候淹。都记。

### 回应 · notebook.notes.silent

```dialogue-choice
{
    "id": "notebook.notes.silent",
    "text": "",
    "next": "silence.notebook",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · notes.private

```dialogue-choice
{
    "id": "notes.private",
    "text": "",
    "next": "notebook.private",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那我不乱翻了。

### 回应 · notes.sea

```dialogue-choice
{
    "id": "notes.sea",
    "text": "",
    "next": "notebook.tide",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 同一个地方，也得分时间记？

### 回应 · notes.leave

```dialogue-choice
{
    "id": "notes.leave",
    "text": "",
    "next": "bye.notebook",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 明白了，你继续记。

## notebook.mess

```dialogue-node
{
    "id": "notebook.mess",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.notebook",
    "topic": "notebook",
    "leaveNext": "bye.notebook",
    "returnEntry": "greet.notebook",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 不麻烦，几笔就够。比白跑一趟省事。

### 回应 · notebook.mess.silent

```dialogue-choice
{
    "id": "notebook.mess.silent",
    "text": "",
    "next": "silence.notebook",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · mess.fish

```dialogue-choice
{
    "id": "mess.fish",
    "text": "",
    "next": "notebook.notes",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 钓鱼的地方也这样记？

### 回应 · mess.leave

```dialogue-choice
{
    "id": "mess.leave",
    "text": "",
    "next": "bye.notebook",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那几笔确实值得记。

## notebook.private

```dialogue-node
{
    "id": "notebook.private",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.notebook",
    "topic": "notebook",
    "leaveNext": "bye.notebook",
    "returnEntry": "greet.notebook",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 没事。我的记号，你也未必看得懂。

### 回应 · notebook.private.silent

```dialogue-choice
{
    "id": "notebook.private.silent",
    "text": "",
    "next": "silence.notebook",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · private.stay

```dialogue-choice
{
    "id": "private.stay",
    "text": "",
    "next": "notebook.quiet",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那你先写，我在旁边待会儿。

### 回应 · private.leave

```dialogue-choice
{
    "id": "private.leave",
    "text": "",
    "next": "bye.notebook",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，下次再聊。

## painting.detail

```dialogue-node
{
    "id": "painting.detail",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 先补这里，水贴着石头的这道阴影。

### 回应 · painting.detail.silent

```dialogue-choice
{
    "id": "painting.detail.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · detail.see

```dialogue-choice
{
    "id": "detail.see",
    "text": "",
    "next": "painting.seen",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 看见了，水边这条窄窄的。

### 回应 · detail.money

```dialogue-choice
{
    "id": "detail.money",
    "text": "",
    "next": "painting.shadow",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 阴影为什么带蓝？

### 回应 · detail.leave

```dialogue-choice
{
    "id": "detail.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 知道是哪儿了，你接着画。

## painting.seen

```dialogue-node
{
    "id": "painting.seen",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 对。接上它，石头才像泡在水里。

### 回应 · painting.seen.silent

```dialogue-choice
{
    "id": "painting.seen.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · seen.stay

```dialogue-choice
{
    "id": "seen.stay",
    "text": "",
    "next": "watch.company",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我想看看你补完会是什么样。

### 回应 · seen.leave

```dialogue-choice
{
    "id": "seen.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 懂了。我下次再看。

## painting.wait

```dialogue-node
{
    "id": "painting.wait",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 嗯。等差不多的光，再接着画。

### 回应 · painting.wait.silent

```dialogue-choice
{
    "id": "painting.wait.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · wait.what

```dialogue-choice
{
    "id": "wait.what",
    "text": "",
    "next": "painting.wait.work",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 等的时候，你做什么？

### 回应 · paintingwait.leave

```dialogue-choice
{
    "id": "paintingwait.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那你慢慢等，我先走。

## painting.unfinished

```dialogue-node
{
    "id": "painting.unfinished",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 先停。等光差不多了再画。

### 回应 · painting.unfinished.silent

```dialogue-choice
{
    "id": "painting.unfinished.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · unfinished.pot

```dialogue-choice
{
    "id": "unfinished.pot",
    "text": "",
    "next": "painting.wait",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 所以得等合适的光？

### 回应 · unfinished.watch

```dialogue-choice
{
    "id": "unfinished.watch",
    "text": "",
    "next": "watch.company",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那我只看看，不催你画完。

### 回应 · unfinished.leave

```dialogue-choice
{
    "id": "unfinished.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 明白了，你按自己的进度来。

## fish.paint

```dialogue-node
{
    "id": "fish.paint",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.fish",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "greet.fish",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 嗯。鱼不一定多钓几条，时间肯定没了。

### 回应 · fish.paint.silent

```dialogue-choice
{
    "id": "fish.paint.silent",
    "text": "",
    "next": "silence.fish",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · fishpaint.sell

```dialogue-choice
{
    "id": "fishpaint.sell",
    "text": "",
    "next": "money.useful",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 不过颜料还是得花钱买。

### 回应 · fishpaint.leave

```dialogue-choice
{
    "id": "fishpaint.leave",
    "text": "",
    "next": "fish.enough",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 嗯，给画画留时间。

## fish.catch

```dialogue-node
{
    "id": "fish.catch",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.fish",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "greet.fish",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 不够就煮点别的。明天再钓。

### 回应 · fish.catch.silent

```dialogue-choice
{
    "id": "fish.catch.silent",
    "text": "",
    "next": "silence.fish",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · catch.tease

```dialogue-choice
{
    "id": "catch.tease",
    "text": "",
    "next": "fish.willing",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你倒不跟它较劲。

### 回应 · catch.paint

```dialogue-choice
{
    "id": "catch.paint",
    "text": "",
    "next": "fish.sketch",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 钓上来的鱼，你也会画吗？

### 回应 · catch.leave

```dialogue-choice
{
    "id": "catch.leave",
    "text": "",
    "next": "fish.enough",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 知道了，你有自己的分寸。

## fish.willing

```dialogue-node
{
    "id": "fish.willing",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.fish",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "greet.fish",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 饿着肚子较劲，也钓不上来。

### 回应 · fish.willing.silent

```dialogue-choice
{
    "id": "fish.willing.silent",
    "text": "",
    "next": "silence.fish",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · willing.sell

```dialogue-choice
{
    "id": "willing.sell",
    "text": "",
    "next": "bye.neutral",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那我不耽误你了。

### 回应 · willing.stop

```dialogue-choice
{
    "id": "willing.stop",
    "text": "",
    "next": "fish.enough",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 行，够吃就收。

## morning.water

```dialogue-node
{
    "id": "morning.water",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.morning",
    "topic": "morning",
    "leaveNext": "bye.morning",
    "returnEntry": "greet.morning",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 还没吃。记完这几笔就去。

### 回应 · morning.water.silent

```dialogue-choice
{
    "id": "morning.water.silent",
    "text": "",
    "next": "silence.morning",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · boil.light

```dialogue-choice
{
    "id": "boil.light",
    "text": "",
    "next": "morning.light",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 记的是哪片颜色？

### 回应 · boil.leave

```dialogue-choice
{
    "id": "boil.leave",
    "text": "",
    "next": "bye.morning",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，记完先去吃饭。

## morning.light

```dialogue-node
{
    "id": "morning.light",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.morning",
    "topic": "morning",
    "leaveNext": "bye.morning",
    "returnEntry": "greet.morning",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 天亮前那片水，偏灰蓝。已经记了几笔。

### 回应 · morning.light.silent

```dialogue-choice
{
    "id": "morning.light.silent",
    "text": "",
    "next": "silence.morning",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · light.detail

```dialogue-choice
{
    "id": "light.detail",
    "text": "",
    "next": "morning.wait",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那明早再接着画？

### 回应 · light.leave

```dialogue-choice
{
    "id": "light.leave",
    "text": "",
    "next": "bye.morning",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 明白，那我先不挡着你。

## night.colour

```dialogue-node
{
    "id": "night.colour",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.night",
    "topic": "night",
    "leaveNext": "bye.night",
    "returnEntry": "greet.night",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 颜色看不准，明暗还行。水画得太白了。

### 回应 · night.colour.silent

```dialogue-choice
{
    "id": "night.colour.silent",
    "text": "",
    "next": "silence.night",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · nightcolour.ask

```dialogue-choice
{
    "id": "nightcolour.ask",
    "text": "",
    "next": "night.last",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那就明天有自然光再改？

### 回应 · nightcolour.stay

```dialogue-choice
{
    "id": "nightcolour.stay",
    "text": "",
    "next": "night.look",
    "speechAct": "listen",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我也看看你说的那块。

### 回应 · nightcolour.leave

```dialogue-choice
{
    "id": "nightcolour.leave",
    "text": "",
    "next": "bye.night",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那你早点收，别熬太晚。

## night.sleep

```dialogue-node
{
    "id": "night.sleep",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.night",
    "topic": "night",
    "leaveNext": "bye.night",
    "returnEntry": "greet.night",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 困。就是收起来之前，总想再看一眼。

### 回应 · night.sleep.silent

```dialogue-choice
{
    "id": "night.sleep.silent",
    "text": "",
    "next": "silence.night",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · sleep.joke

```dialogue-choice
{
    "id": "sleep.joke",
    "text": "",
    "next": "night.last",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那就留到明天，今晚先收吧。

### 回应 · sleep.leave

```dialogue-choice
{
    "id": "sleep.leave",
    "text": "",
    "next": "bye.night",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你早点休息，晚安。

## night.last

```dialogue-node
{
    "id": "night.last",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "night",
    "leaveNext": "bye.night",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，今晚不改了。明天再看。

## watch.company

```dialogue-node
{
    "id": "watch.company",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 好，你慢慢看。我先不说了。

## silence.painting

```dialogue-node
{
    "id": "silence.painting",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "我接着画了，你随意。",
        "你慢慢看，我先忙手上的。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我先接着画，你随意。

## silence.meal

```dialogue-node
{
    "id": "silence.meal",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我先忙手边的事了。

## silence.night

```dialogue-node
{
    "id": "silence.night",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "night",
    "leaveNext": "bye.night",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我先收起来了。

## bye.painting

```dialogue-node
{
    "id": "bye.painting",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "好，我接着画。留神那些纸。",
        "嗯，我先画了。别踩着脚边的纸。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 好，我接着画了。留神脚边的画纸。

## bye.passing

```dialogue-node
{
    "id": "bye.passing",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "neutral",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，慢走。

## bye.promise

```dialogue-node
{
    "id": "bye.promise",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 好。那件事有变化，告诉我一声就行。

## bye.tent

```dialogue-node
{
    "id": "bye.tent",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，我再看看这根绳子。

## bye.night

```dialogue-node
{
    "id": "bye.night",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "night",
    "leaveNext": "bye.night",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 晚安。我也准备收了。

## bye.meal

```dialogue-node
{
    "id": "bye.meal",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，我先去弄点吃的。

## sale.respect

```dialogue-node
{
    "id": "sale.respect",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯。这张我还想留着。

## sale.keep

```dialogue-node
{
    "id": "sale.keep",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 刚来时画的第一张。那张不卖。

## paint.shop.confirm

```dialogue-node
{
    "id": "paint.shop.confirm",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 对，那家。一管钴蓝，没有就算了。

## paint.noappointment

```dialogue-node
{
    "id": "paint.noappointment",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 行，没约时间。谢谢你帮我留意。

## painting.stone

```dialogue-node
{
    "id": "painting.stone",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 先画石头的暖灰。水和阴影等等。

## paint.received

```dialogue-node
{
    "id": "paint.received",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，终于不用再挤旧管子了。

## painting.shadow

```dialogue-node
{
    "id": "painting.shadow",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 水会映上蓝色。只涂黑，就像分开了。

## tent.focus

```dialogue-node
{
    "id": "tent.focus",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 好，等布稳住我就绑。

## tent.stone

```dialogue-node
{
    "id": "tent.stone",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 能，就是挪起来慢一点。我慢慢绑，不急。

## night.look

```dialogue-node
{
    "id": "night.look",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "night",
    "leaveNext": "bye.night",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 中间那片水，太亮了。明天再压暗点。

## paint.waiting

```dialogue-node
{
    "id": "paint.waiting",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 水和阴影停着了。别的还能画。

### 回应 · paint.waiting.silent

```dialogue-choice
{
    "id": "paint.waiting.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · waiting.cancel

```dialogue-choice
{
    "id": "waiting.cancel",
    "text": "",
    "next": "paint.release",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那取消吧，别再等我。

### 回应 · waiting.keep

```dialogue-choice
{
    "id": "waiting.keep",
    "text": "",
    "next": "promise.late",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我还会带来，先不约新时间。

## tent.finished

```dialogue-node
{
    "id": "tent.finished",
    "text": "",
    "fallback": "tent.wait",
    "effect": "",
    "silenceNext": "",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [
        {
            "op": "fact",
            "id": "tent_done",
            "value": "true"
        }
    ],
    "choices": []
}
```

> 嗯，总算能腾出手了。谢谢。

## notebook.tide

```dialogue-node
{
    "id": "notebook.tide",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "notebook",
    "leaveNext": "bye.notebook",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯。只记地点，换个时候就可能踩进水。

## notebook.quiet

```dialogue-node
{
    "id": "notebook.quiet",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "notebook",
    "leaveNext": "bye.notebook",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，我把今天这一行写完。

## fish.sketch

```dialogue-node
{
    "id": "fish.sketch",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 会，画几笔就收。还得趁新鲜吃。

## fish.enough

```dialogue-node
{
    "id": "fish.enough",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 对，吃饭和画画都顾上就好。

## morning.wait

```dialogue-node
{
    "id": "morning.wait",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "morning",
    "leaveNext": "bye.morning",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯。明早差不多的时候，接着画。

## bye.neutral

```dialogue-node
{
    "id": "bye.neutral",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "neutral",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 好，回头聊。

## bye.sale

```dialogue-node
{
    "id": "bye.sale",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，画的事我再想想。

## bye.paint

```dialogue-node
{
    "id": "bye.paint",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 好，颜料的事就先说到这儿。

## bye.fish

```dialogue-node
{
    "id": "bye.fish",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，回头聊。

## bye.morning

```dialogue-node
{
    "id": "bye.morning",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "morning",
    "leaveNext": "bye.morning",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，我先忙了，回头聊。

## bye.notebook

```dialogue-node
{
    "id": "bye.notebook",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "notebook",
    "leaveNext": "bye.notebook",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，我把这一行写完。

## silence.neutral

```dialogue-node
{
    "id": "silence.neutral",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "neutral",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 那先这样。

## silence.sale

```dialogue-node
{
    "id": "silence.sale",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 这件事我还得想想，先不说了。

## silence.paint

```dialogue-node
{
    "id": "silence.paint",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 颜料的事先说到这儿吧。

## silence.fish

```dialogue-node
{
    "id": "silence.fish",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "fish",
    "leaveNext": "bye.fish",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我先忙手边的事了。

## silence.morning

```dialogue-node
{
    "id": "silence.morning",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "morning",
    "leaveNext": "bye.morning",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我先记颜色，免得一会儿忘了。

## silence.notebook

```dialogue-node
{
    "id": "silence.notebook",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "notebook",
    "leaveNext": "bye.notebook",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我先把这行记完。

## silence.tent

```dialogue-node
{
    "id": "silence.tent",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我先弄这根绳子。

## return.painting

```dialogue-node
{
    "id": "return.painting",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.painting",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "return.painting",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 还想看那片水？我指给你。

### 回应 · return.painting.silent

```dialogue-choice
{
    "id": "return.painting.silent",
    "text": "",
    "next": "silence.painting",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · rpainting.where

```dialogue-choice
{
    "id": "rpainting.where",
    "text": "",
    "next": "painting.detail",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，指给我看看。

### 回应 · rpainting.leave

```dialogue-choice
{
    "id": "rpainting.leave",
    "text": "",
    "next": "bye.neutral",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 不用，我只是经过。

## return.paint

```dialogue-node
{
    "id": "return.paint",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 颜料的事，还有想问的吗？

### 回应 · return.paint.silent

```dialogue-choice
{
    "id": "return.paint.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · rpaint.shop

```dialogue-choice
{
    "id": "rpaint.shop",
    "text": "",
    "next": "paint.shop",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 再跟我说一下去哪买。

### 回应 · rpaint.leave

```dialogue-choice
{
    "id": "rpaint.leave",
    "text": "",
    "next": "bye.paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 没了，回头聊。

## return.sale

```dialogue-node
{
    "id": "return.sale",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "silence.sale",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "return.sale",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 卖画的事，我还没想好。

### 回应 · return.sale.silent

```dialogue-choice
{
    "id": "return.sale.silent",
    "text": "",
    "next": "silence.sale",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · rsale.concern

```dialogue-choice
{
    "id": "rsale.concern",
    "text": "",
    "next": "money.shy",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你最担心的是什么？

### 回应 · rsale.leave

```dialogue-choice
{
    "id": "rsale.leave",
    "text": "",
    "next": "sale.respect",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那你慢慢想。

## return.tent

```dialogue-node
{
    "id": "return.tent",
    "text": "",
    "fallback": "greet.tent",
    "effect": "",
    "silenceNext": "silence.tent",
    "topic": "tent",
    "leaveNext": "bye.tent",
    "returnEntry": "return.tent",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "tent_done",
            "value": "false"
        }
    ],
    "choices": []
}
```

> 还是这根绳子。要帮忙就压住那块布。

### 回应 · return.tent.silent

```dialogue-choice
{
    "id": "return.tent.silent",
    "text": "",
    "next": "silence.tent",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · rtent.help

```dialogue-choice
{
    "id": "rtent.help",
    "text": "",
    "next": "tent.wait",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，我过去。

### 回应 · rtent.leave

```dialogue-choice
{
    "id": "rtent.leave",
    "text": "",
    "next": "bye.tent",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我只是经过，你先忙。

## sale.private

```dialogue-node
{
    "id": "sale.private",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "sale",
    "leaveNext": "bye.sale",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 你看就好。卖不卖，我还不想谈。

## painting.moments

```dialogue-node
{
    "id": "painting.moments",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯。并排放着，就能看出光变了多少。

## painting.wait.work

```dialogue-node
{
    "id": "painting.wait.work",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 洗笔，整理画纸。也不算干等。

## paint.recall.detail

```dialogue-node
{
    "id": "paint.recall.detail",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "",
    "topic": "painting",
    "leaveNext": "bye.painting",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 这道阴影。加了你那管蓝，水才接上。

## money.supplied

```dialogue-node
{
    "id": "money.supplied",
    "text": "",
    "fallback": "money.useful",
    "effect": "",
    "silenceNext": "silence.paint",
    "topic": "paint",
    "leaveNext": "bye.paint",
    "returnEntry": "return.paint",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [
        {
            "op": "fact",
            "id": "needs_paint",
            "value": "false"
        }
    ],
    "choices": []
}
```

> 嗯，颜料得买。蓝够用了，鱼能自己钓。

### 回应 · money.supplied.silent

```dialogue-choice
{
    "id": "money.supplied.silent",
    "text": "",
    "next": "silence.paint",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · supplied.fish

```dialogue-choice
{
    "id": "supplied.fish",
    "text": "",
    "next": "fish",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你吃的鱼都是自己钓的？

### 回应 · supplied.leave

```dialogue-choice
{
    "id": "supplied.leave",
    "text": "",
    "next": "bye.painting",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那不耽误你画了。

## observe.quiet

```dialogue-node
{
    "id": "observe.quiet",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "quiet",
    "textVariants": [
        "你这会儿挺安静。想静静也行。",
        "好一会儿没听你说话了。不急着说。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 你今天话不多。想安静一会儿？

### 回应 · observe.quiet.silent

```dialogue-choice
{
    "id": "observe.quiet.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · quiet.yes

```dialogue-choice
{
    "id": "quiet.yes",
    "text": "",
    "next": "company.quiet",
    "speechAct": "",
    "signal": "self.quiet",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 嗯，今天不想说话。

### 回应 · quiet.think

```dialogue-choice
{
    "id": "quiet.think",
    "text": "",
    "next": "company.thinking",
    "speechAct": "",
    "signal": "self.thinking",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我在想事情。

### 回应 · quiet.chat

```dialogue-choice
{
    "id": "quiet.chat",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 其实想聊，就是不知说什么。

### 回应 · quiet.brush

```dialogue-choice
{
    "id": "quiet.brush",
    "text": "",
    "next": "company.brush",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 在看你怎么落笔。

## observe.speech

```dialogue-node
{
    "id": "observe.speech",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "speech",
    "textVariants": [
        "不知不觉聊了好一会儿。",
        "跟你说着话，时间倒过得快。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 聊了不少。还想再待会儿？

### 回应 · observe.speech.silent

```dialogue-choice
{
    "id": "observe.speech.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · speech.yes

```dialogue-choice
{
    "id": "speech.yes",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 再说会儿吧。

### 回应 · speech.ask

```dialogue-choice
{
    "id": "speech.ask",
    "text": "",
    "next": "company.voice",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 一开口就说远了。

### 回应 · speech.leave

```dialogue-choice
{
    "id": "speech.leave",
    "text": "",
    "next": "bye.neutral",
    "speechAct": "",
    "signal": "self.passing",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 够了，我去走走。

### 回应 · speech.time

```dialogue-choice
{
    "id": "speech.time",
    "text": "",
    "next": "company.time",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 没耽误你吧？

## observe.question

```dialogue-node
{
    "id": "observe.question",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "question",
    "textVariants": [
        "你问得比我想的细。",
        "这些小地方，你倒都问到了。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 你问得挺细。平时也这么好奇？

### 回应 · observe.question.silent

```dialogue-choice
{
    "id": "observe.question.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · question.yes

```dialogue-choice
{
    "id": "question.yes",
    "text": "",
    "next": "company.curious",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 不懂就顺嘴问了。

### 回应 · question.no

```dialogue-choice
{
    "id": "question.no",
    "text": "",
    "next": "company.daily",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 只对你这儿好奇。

### 回应 · question.bored

```dialogue-choice
{
    "id": "question.bored",
    "text": "",
    "next": "company.bored",
    "speechAct": "",
    "signal": "self.bored",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 其实是闲得没事。

### 回应 · question.detail

```dialogue-choice
{
    "id": "question.detail",
    "text": "",
    "next": "company.detail",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你不说，还真注意不到。

## observe.art

```dialogue-node
{
    "id": "observe.art",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "art",
    "textVariants": [
        "看你问这些，还以为你也画画。",
        "你倒一直在看画。平时也画两笔？"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 你一直在问画。自己也画？

### 回应 · observe.art.silent

```dialogue-choice
{
    "id": "observe.art.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · art.yes

```dialogue-choice
{
    "id": "art.yes",
    "text": "",
    "next": "company.draws",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 偶尔画两笔。

### 回应 · art.no

```dialogue-choice
{
    "id": "art.no",
    "text": "",
    "next": "company.look",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 不会画，就是想看懂。

### 回应 · art.rest

```dialogue-choice
{
    "id": "art.rest",
    "text": "",
    "next": "company.copy",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 以前照着临过一点。

### 回应 · art.blue

```dialogue-choice
{
    "id": "art.blue",
    "text": "",
    "next": "company.blue",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这片蓝挺好看的。

## observe.practical

```dialogue-node
{
    "id": "observe.practical",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "practical",
    "textVariants": [
        "连这些琐碎事也问。你也常在外头待？",
        "绳子、颜料这些，你还挺留意。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 你挺关心这些小事。也在外头住？

### 回应 · observe.practical.silent

```dialogue-choice
{
    "id": "observe.practical.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · practical.yes

```dialogue-choice
{
    "id": "practical.yes",
    "text": "",
    "next": "company.learn",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这些我还真没自己弄过。

### 回应 · practical.no

```dialogue-choice
{
    "id": "practical.no",
    "text": "",
    "next": "company.daily",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 没有，听着新鲜。

### 回应 · practical.help

```dialogue-choice
{
    "id": "practical.help",
    "text": "",
    "next": "company.help",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 看看有没有能帮你的。

### 回应 · practical.knots

```dialogue-choice
{
    "id": "practical.knots",
    "text": "",
    "next": "company.knots",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 原来绳头也有讲究。

## observe.care

```dialogue-node
{
    "id": "observe.care",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "care",
    "textVariants": [
        "你还记得问我吃没吃饭啊。",
        "画的事没问完，先问到我睡觉了。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 你常问吃饭休息的事。怕我忘了？

### 回应 · observe.care.silent

```dialogue-choice
{
    "id": "observe.care.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · care.yes

```dialogue-choice
{
    "id": "care.yes",
    "text": "",
    "next": "company.care",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你画起来好像顾不上。

### 回应 · care.no

```dialogue-choice
{
    "id": "care.no",
    "text": "",
    "next": "company.casual",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 就是顺口一问。

### 回应 · care.rest

```dialogue-choice
{
    "id": "care.rest",
    "text": "",
    "next": "company.rest",
    "speechAct": "",
    "signal": "self.rest",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我自己倒想歇会儿。

### 回应 · care.meal

```dialogue-choice
{
    "id": "care.meal",
    "text": "",
    "next": "company.meal",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 画到一半，很难停吧？

## observe.help

```dialogue-node
{
    "id": "observe.help",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "help",
    "textVariants": [
        "今天倒省了不少力气。谢谢你。",
        "颜料和那块布，都多亏你搭手。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 送颜料、帮压布。你帮我不少。

### 回应 · observe.help.silent

```dialogue-choice
{
    "id": "observe.help.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · help.yes

```dialogue-choice
{
    "id": "help.yes",
    "text": "",
    "next": "company.helped",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 也没费多少事。

### 回应 · help.bored

```dialogue-choice
{
    "id": "help.bored",
    "text": "",
    "next": "company.bored",
    "speechAct": "",
    "signal": "self.bored",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 正好也没事做。

### 回应 · help.chat

```dialogue-choice
{
    "id": "help.chat",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 其实也是想找你聊聊。

### 回应 · help.hands

```dialogue-choice
{
    "id": "help.hands",
    "text": "",
    "next": "company.hands",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 刚才那根绳子是真滑。

## observe.return

```dialogue-node
{
    "id": "observe.return",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "return",
    "textVariants": [
        "又碰上了。这一圈不算远吧？",
        "才分开一会儿，又见着你了。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 又绕回来了。是想找人聊聊？

### 回应 · observe.return.silent

```dialogue-choice
{
    "id": "observe.return.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · return.chat

```dialogue-choice
{
    "id": "return.chat",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 嗯，一个人待着有点闷。

### 回应 · return.bored

```dialogue-choice
{
    "id": "return.bored",
    "text": "",
    "next": "company.bored",
    "speechAct": "",
    "signal": "self.bored",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 不知道去哪儿，随便转转。

### 回应 · return.no

```dialogue-choice
{
    "id": "return.no",
    "text": "",
    "next": "company.passing",
    "speechAct": "",
    "signal": "self.passing",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 只是这条路顺。

### 回应 · return.loop

```dialogue-choice
{
    "id": "return.loop",
    "text": "",
    "next": "company.shortloop",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 绕一圈就又到这儿了。

## observe.company

```dialogue-node
{
    "id": "observe.company",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "company",
    "textVariants": [
        "还在这儿呢。这地方待着还行吧？",
        "一抬头，你还在。站累了没有？"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 你在这儿待了一会儿了。想歇歇？

### 回应 · observe.company.silent

```dialogue-choice
{
    "id": "observe.company.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · company.yes

```dialogue-choice
{
    "id": "company.yes",
    "text": "",
    "next": "company.rest",
    "speechAct": "",
    "signal": "self.rest",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 嗯，待着就挺好。

### 回应 · company.quietchoice

```dialogue-choice
{
    "id": "company.quietchoice",
    "text": "",
    "next": "company.quiet",
    "speechAct": "",
    "signal": "self.quiet",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 想安静看一会儿。

### 回应 · company.boredchoice

```dialogue-choice
{
    "id": "company.boredchoice",
    "text": "",
    "next": "company.bored",
    "speechAct": "",
    "signal": "self.bored",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 有点无聊，不知道干什么。

### 回应 · company.waves

```dialogue-choice
{
    "id": "company.waves",
    "text": "",
    "next": "company.waves",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 一直在听水声。

## observe.hasty

```dialogue-node
{
    "id": "observe.hasty",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "hasty",
    "textVariants": [
        "你每回都来去匆匆的。",
        "这几回，你都没停多久。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 这几次都走得挺快。在忙？

### 回应 · observe.hasty.silent

```dialogue-choice
{
    "id": "observe.hasty.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · hasty.yes

```dialogue-choice
{
    "id": "hasty.yes",
    "text": "",
    "next": "company.passing",
    "speechAct": "",
    "signal": "self.passing",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 嗯，先不聊了。

### 回应 · hasty.no

```dialogue-choice
{
    "id": "hasty.no",
    "text": "",
    "next": "company.bored",
    "speechAct": "",
    "signal": "self.bored",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 没事，就是待不住。

### 回应 · hasty.chat

```dialogue-choice
{
    "id": "hasty.chat",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 现在不急，可以聊会儿。

### 回应 · hasty.winded

```dialogue-choice
{
    "id": "hasty.winded",
    "text": "",
    "next": "company.winded",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 刚才那段路走得有点急。

## observe.self.quiet

```dialogue-node
{
    "id": "observe.self.quiet",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "self.quiet",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 还想安静待着？我不多问。

### 回应 · observe.self.quiet.silent

```dialogue-choice
{
    "id": "observe.self.quiet.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · squiet.yes

```dialogue-choice
{
    "id": "squiet.yes",
    "text": "",
    "next": "company.quiet",
    "speechAct": "",
    "signal": "self.quiet",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 嗯。

### 回应 · squiet.change

```dialogue-choice
{
    "id": "squiet.change",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 现在倒想聊两句。

### 回应 · squiet.wind

```dialogue-choice
{
    "id": "squiet.wind",
    "text": "",
    "next": "company.wind",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 风小了。

## observe.self.chat

```dialogue-node
{
    "id": "observe.self.chat",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "self.chat",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 上回你说想聊聊。今天还想听什么？

### 回应 · observe.self.chat.silent

```dialogue-choice
{
    "id": "observe.self.chat.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · schat.paint

```dialogue-choice
{
    "id": "schat.paint",
    "text": "",
    "next": "watch",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 说说这幅画吧。

### 回应 · schat.daily

```dialogue-choice
{
    "id": "schat.daily",
    "text": "",
    "next": "company.daily",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 说点日常的小事。

### 回应 · schat.change

```dialogue-choice
{
    "id": "schat.change",
    "text": "",
    "next": "company.quiet",
    "speechAct": "",
    "signal": "self.quiet",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 今天想静静。

### 回应 · schat.lost

```dialogue-choice
{
    "id": "schat.lost",
    "text": "",
    "next": "company.lostword",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 刚才想说什么，突然忘了。

## observe.self.bored

```dialogue-node
{
    "id": "observe.self.bored",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "self.bored",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 还没找到想做的事？

### 回应 · observe.self.bored.silent

```dialogue-choice
{
    "id": "observe.self.bored.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · sbored.yes

```dialogue-choice
{
    "id": "sbored.yes",
    "text": "",
    "next": "company.activity",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 没有。你有主意吗？

### 回应 · sbored.no

```dialogue-choice
{
    "id": "sbored.no",
    "text": "",
    "next": "company.passing",
    "speechAct": "",
    "signal": "self.passing",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 有了，只是顺路看看你。

### 回应 · sbored.chat

```dialogue-choice
{
    "id": "sbored.chat",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 不找了，聊会儿就好。

### 回应 · sbored.waves

```dialogue-choice
{
    "id": "sbored.waves",
    "text": "",
    "next": "company.waves",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 就听听水声，也挺好。

## observe.self.rest

```dialogue-node
{
    "id": "observe.self.rest",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "self.rest",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 又来歇脚？这儿还空着。

### 回应 · observe.self.rest.silent

```dialogue-choice
{
    "id": "observe.self.rest.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · srest.yes

```dialogue-choice
{
    "id": "srest.yes",
    "text": "",
    "next": "company.rest",
    "speechAct": "",
    "signal": "self.rest",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 嗯，借地方待会儿。

### 回应 · srest.no

```dialogue-choice
{
    "id": "srest.no",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这回想找你聊天。

### 回应 · srest.time

```dialogue-choice
{
    "id": "srest.time",
    "text": "",
    "next": "company.rest",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你忙你的，不用管我。

## observe.self.passing

```dialogue-node
{
    "id": "observe.self.passing",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "self.passing",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 路过的话，留神脚边的纸。

### 回应 · observe.self.passing.silent

```dialogue-choice
{
    "id": "observe.self.passing.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · spassing.yes

```dialogue-choice
{
    "id": "spassing.yes",
    "text": "",
    "next": "bye.passing",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 好，我绕开。

### 回应 · spassing.no

```dialogue-choice
{
    "id": "spassing.no",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 这回是来找你聊的。

### 回应 · spassing.paper

```dialogue-choice
{
    "id": "spassing.paper",
    "text": "",
    "next": "company.paperweight",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 纸压稳了吧？

## observe.self.thinking

```dialogue-node
{
    "id": "observe.self.thinking",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "self.thinking",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 上回想的事，还没理清？

### 回应 · observe.self.thinking.silent

```dialogue-choice
{
    "id": "observe.self.thinking.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · sthink.yes

```dialogue-choice
{
    "id": "sthink.yes",
    "text": "",
    "next": "company.activity",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 没，想换换脑子。

### 回应 · sthink.no

```dialogue-choice
{
    "id": "sthink.no",
    "text": "",
    "next": "company.sorted",
    "speechAct": "",
    "signal": "self.clear",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 想通了。

### 回应 · sthink.private

```dialogue-choice
{
    "id": "sthink.private",
    "text": "",
    "next": "company.quiet",
    "speechAct": "",
    "signal": "self.quiet",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 暂时不想说。

### 回应 · sthink.lost

```dialogue-choice
{
    "id": "sthink.lost",
    "text": "",
    "next": "company.thinking",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 越想越乱，先放一放。

## company.start

```dialogue-node
{
    "id": "company.start",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "这张画磨了我好一会儿。",
        "刚才还在跟水边这块较劲。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 刚才还在跟这张画较劲。

### 回应 · company.start.silent

```dialogue-choice
{
    "id": "company.start.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · start.art

```dialogue-choice
{
    "id": "start.art",
    "text": "",
    "next": "watch",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 哪里不顺？

### 回应 · start.life

```dialogue-choice
{
    "id": "start.life",
    "text": "",
    "next": "company.daily",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 歇一下，说点别的？

### 回应 · start.you

```dialogue-choice
{
    "id": "start.you",
    "text": "",
    "next": "company.alone",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 你一个人在这儿，不闷吗？

### 回应 · start.nearly

```dialogue-choice
{
    "id": "start.nearly",
    "text": "",
    "next": "company.nearly",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 看着已经差不多了。

## company.daily

```dialogue-node
{
    "id": "company.daily",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "今早钓鱼，钓着块石头。线挂住了。",
        "说件小事，今早把鱼线挂石头上了。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 那说件小事：今早鱼线挂石头上了。

### 回应 · company.daily.silent

```dialogue-choice
{
    "id": "company.daily.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · daily.line

```dialogue-choice
{
    "id": "daily.line",
    "text": "",
    "next": "company.line",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 后来怎么弄下来的？

### 回应 · daily.laugh

```dialogue-choice
{
    "id": "daily.laugh",
    "text": "",
    "next": "company.fishmistake",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 还以为是条大鱼？

## company.line

```dialogue-node
{
    "id": "company.line",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 退两步，松线。拉得越急，挂得越紧。

## company.fishmistake

```dialogue-node
{
    "id": "company.fishmistake",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，还高兴了一下。白费劲。

## company.alone

```dialogue-node
{
    "id": "company.alone",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 偶尔。有人来就聊，没人来也过得下去。

### 回应 · company.alone.silent

```dialogue-choice
{
    "id": "company.alone.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · alone.stay

```dialogue-choice
{
    "id": "alone.stay",
    "text": "",
    "next": "company.rest",
    "speechAct": "",
    "signal": "self.rest",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 那我陪你待会儿。

### 回应 · alone.no

```dialogue-choice
{
    "id": "alone.no",
    "text": "",
    "next": "company.welcome",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我就不太习惯一个人。

## company.welcome

```dialogue-node
{
    "id": "company.welcome",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 那有空就来。不必每回都找个话题。

## company.curious

```dialogue-node
{
    "id": "company.curious",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 挺好。不懂就问，我也常问画材店的人。

## company.draws

```dialogue-node
{
    "id": "company.draws",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 那你知道，想画和画出来是两回事。

### 回应 · company.draws.silent

```dialogue-choice
{
    "id": "company.draws.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · draws.yes

```dialogue-choice
{
    "id": "draws.yes",
    "text": "",
    "next": "company.practice",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 对，总不太像。

### 回应 · draws.no

```dialogue-choice
{
    "id": "draws.no",
    "text": "",
    "next": "company.ownway",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我倒不太在乎像不像。

## company.practice

```dialogue-node
{
    "id": "company.practice",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我也一样。有时留着，过几天才看懂哪儿错。

## company.ownway

```dialogue-node
{
    "id": "company.ownway",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 那也好。画给自己看，不用照我的来。

## company.look

```dialogue-node
{
    "id": "company.look",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 不用急着看懂。先看哪块让你想多看一眼。

### 回应 · company.look.silent

```dialogue-choice
{
    "id": "company.look.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · look.water

```dialogue-choice
{
    "id": "look.water",
    "text": "",
    "next": "company.shared",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 中间那片水。

### 回应 · look.nothing

```dialogue-choice
{
    "id": "look.nothing",
    "text": "",
    "next": "company.nothing",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 暂时没有。

## company.nothing

```dialogue-node
{
    "id": "company.nothing",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 那就没有。不用替我找好话。

## company.learn

```dialogue-node
{
    "id": "company.learn",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 那先记一条：绑绳子别把绳头留太短。

### 回应 · company.learn.silent

```dialogue-choice
{
    "id": "company.learn.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · learn.why

```dialogue-choice
{
    "id": "learn.why",
    "text": "",
    "next": "company.knot",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 容易松？

### 回应 · learn.ok

```dialogue-choice
{
    "id": "learn.ok",
    "text": "",
    "next": "company.learned",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 记住了。

## company.learned

```dialogue-node
{
    "id": "company.learned",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯。我就是松过一次才记住的。

## company.help

```dialogue-node
{
    "id": "company.help",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 看看手边的事吧。

### 回应 · company.help.silent

```dialogue-choice
{
    "id": "company.help.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · help.tent

```dialogue-choice
{
    "id": "help.tent",
    "text": "",
    "next": "tent",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 帐篷还需要帮忙吗？

### 回应 · help.paint

```dialogue-choice
{
    "id": "help.paint",
    "text": "",
    "next": "paint",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 颜料够不够？

## company.helped

```dialogue-node
{
    "id": "company.helped",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "省了我不少事。谢了。",
        "有人搭把手，果然快些。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯。有你搭手，省了不少事。

## company.care

```dialogue-node
{
    "id": "company.care",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 有时候会。不过饿了还是会停笔。

## company.casual

```dialogue-node
{
    "id": "company.casual",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，我还以为你在替我操心。

## company.bored

```dialogue-node
{
    "id": "company.bored",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "来得正好，我正看这张画。",
        "我也正闲下来，看看画而已。"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 正好，我在看这张画。

### 回应 · company.bored.silent

```dialogue-choice
{
    "id": "company.bored.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · bored.do

```dialogue-choice
{
    "id": "bored.do",
    "text": "",
    "next": "company.activity",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 有什么要我帮着看的？

### 回应 · bored.chat

```dialogue-choice
{
    "id": "bored.chat",
    "text": "",
    "next": "company.start",
    "speechAct": "",
    "signal": "self.chat",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 先歇会儿，聊两句。

### 回应 · bored.rest

```dialogue-choice
{
    "id": "bored.rest",
    "text": "",
    "next": "company.rest",
    "speechAct": "",
    "signal": "self.rest",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 我先在这儿待着。

### 回应 · bored.paper

```dialogue-choice
{
    "id": "bored.paper",
    "text": "",
    "next": "company.paper",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 边上那张纸还留着？

## company.activity

```dialogue-node
{
    "id": "company.activity",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "帮我看看？哪一块最抢眼？",
        "这张你看一下，先注意到哪里？"
    ],
    "endAfterSeconds": 0.0,
    "responseSeconds": 36.0,
    "conditions": [],
    "choices": []
}
```

> 看看这张画？帮我挑块最扎眼的。

### 回应 · company.activity.silent

```dialogue-choice
{
    "id": "company.activity.silent",
    "text": "",
    "next": "company.quiet",
    "speechAct": "silence",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> ...

### 回应 · activity.water

```dialogue-choice
{
    "id": "activity.water",
    "text": "",
    "next": "company.bright",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 中间那片水太亮。

### 回应 · activity.stone

```dialogue-choice
{
    "id": "activity.stone",
    "text": "",
    "next": "company.edge",
    "speechAct": "",
    "signal": "",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 石头的边太硬。

### 回应 · activity.no

```dialogue-choice
{
    "id": "activity.no",
    "text": "",
    "next": "company.passing",
    "speechAct": "",
    "signal": "self.passing",
    "expiresAfterSeconds": 0.0,
    "conditions": []
}
```

> 算了，我还是去走走。

## company.bright

```dialogue-node
{
    "id": "company.bright",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我也觉得。先记着，等光合适再改。

## company.edge

```dialogue-node
{
    "id": "company.edge",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，边上该松一点。不用每条都描清楚。

## company.rest

```dialogue-node
{
    "id": "company.rest",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "行，我接着忙。你待着就好。",
        "在这儿歇吧，不用管我。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 待着吧。我忙我的，不用招呼我。

## company.quiet

```dialogue-node
{
    "id": "company.quiet",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "嗯，你随意。我先忙手上的。",
        "那就待一会儿，不说话也行。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，不用非得说点什么。

## company.thinking

```dialogue-node
{
    "id": "company.thinking",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 那慢慢想。我先不问。

## company.passing

```dialogue-node
{
    "id": "company.passing",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "好，路上慢点。",
        "嗯，回头见。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 明白，慢走。

## company.sorted

```dialogue-node
{
    "id": "company.sorted",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 那就好。不用特地讲给我听。

## company.knot

```dialogue-node
{
    "id": "company.knot",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，绳头短，风一扯就容易滑。

## company.shared

```dialogue-node
{
    "id": "company.shared",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，我也先看那儿。水的颜色总在变。

## company.brush

```dialogue-node
{
    "id": "company.brush",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "这儿轻轻带过去就好。画死了不好改。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 在看落笔啊。这笔轻点，边就软些。

## company.wind

```dialogue-node
{
    "id": "company.wind",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "难得风小一点，画纸总算老实了。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，风是比刚才小了。纸也不乱跑了。

## company.voice

```dialogue-node
{
    "id": "company.voice",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "聊着聊着就远了，我也常这样。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我也是。手上画着，嘴就跟着说了。

## company.time

```dialogue-node
{
    "id": "company.time",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "没事，这会儿本来也得停一停。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 没耽误。正好等这层干一点。

## company.detail

```dialogue-node
{
    "id": "company.detail",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "你一问，我才发现自己也没细想。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 有些我也没想过，被你一问才注意。

## company.copy

```dialogue-node
{
    "id": "company.copy",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "那你知道，看着容易，落笔就变样。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 临过也有用。至少知道笔不太听话。

## company.blue

```dialogue-node
{
    "id": "company.blue",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "是旁边的颜色衬着，单看没这么蓝。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，旁边暖一点，这片蓝就显出来了。

## company.knots

```dialogue-node
{
    "id": "company.knots",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "真被风吹松一次，就会记着了。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 我也是松过几次，才开始留意绳头。

## company.meal

```dialogue-node
{
    "id": "company.meal",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "有时候真忘。肚子叫了才停。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 是会画过头。等饿了，手也画不稳。

## company.hands

```dialogue-node
{
    "id": "company.hands",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "就缺那一下。不然我两只手不够用。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 你刚才那一下，正好让我腾出手。

## company.shortloop

```dialogue-node
{
    "id": "company.shortloop",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "嗯，沿这边走，不一会儿就回来了。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 这边绕一圈，很快又是这块石头。

## company.waves

```dialogue-node
{
    "id": "company.waves",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "我有时画着画着，也会跟着听。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 听久了，会等着下一下。

## company.winded

```dialogue-node
{
    "id": "company.winded",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "歇着吧。我这儿也没什么急事。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 那缓一会儿，不急。

## company.lostword

```dialogue-node
{
    "id": "company.lostword",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "我也常这样，话到嘴边又没了。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 没事，等想起来再说。

## company.paper

```dialogue-node
{
    "id": "company.paper",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "没扔。翻过来，还能试几笔。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，背面还能试颜色。先留着。

## company.nearly

```dialogue-node
{
    "id": "company.nearly",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "总觉得快了，再看又想改一笔。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 差一点。每回都觉得只差一点。

## company.paperweight

```dialogue-node
{
    "id": "company.paperweight",
    "text": "",
    "fallback": "topics",
    "effect": "",
    "silenceNext": "company.quiet",
    "topic": "company",
    "leaveNext": "bye.neutral",
    "returnEntry": "",
    "observation": "",
    "textVariants": [
        "压着了，这会儿还稳。"
    ],
    "endAfterSeconds": 4.0,
    "responseSeconds": 0.0,
    "conditions": [],
    "choices": []
}
```

> 嗯，石头压着呢。只要风别太大。


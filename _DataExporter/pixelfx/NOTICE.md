# pixelfx 에서 떠 온 이펙트 프레임

아래 이펙트의 `Assets/_Art/FX/<폴더>/core · glow` 는 pixelfx (https://github.com/patch-gg/pixelfx, 커밋 a841596) 로 그린 그림이다.
`tools/pixelfx_import.mjs` 가 이 폴더에 중간 프레임(`<폴더>/NN.png`, 저장소 제외)을 뽑고 `build_vfx.py` 가 굽는다.
중간 프레임이 없으면 `build_vfx.py` 는 이미 구운 그림을 그대로 둔다.

| 폴더 | pixelfx 이펙트 |
|---|---|
| fx_iaido | melee/sword-slash (벚꽃 분홍 팔레트) |

pixelfx 는 MIT 라이선스다. 출시 크레딧에도 같은 고지를 싣는다.

```
MIT License

Copyright (c) 2026 Patch
Copyright (c) 2026 Hugo Duprez

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

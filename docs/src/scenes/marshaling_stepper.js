// marshaling_stepper.js — Canvas2D scene plugin for Section 6
// 마샬링 생성자의 8가지 필드 타입별 코드 패턴을 단계별로 보여주는 인터랙티브 스테퍼
// 왼쪽: FieldNode 속성 카드, 오른쪽: 생성되는 C++ 코드 하이라이트
// Type: canvas2d | Section 6 of AST_to_Unreal_설명

module.exports = {
  type: 'canvas2d',

  html: function(ids) {
    var P = ids.canvas;

    // --- Scoped styles ---
    var style = '<style>\n' +
      '.s6-wrap{display:flex;gap:14px;flex-wrap:wrap;}\n' +
      '.s6-left{flex:0 0 280px;min-width:250px;max-width:320px;}\n' +
      '.s6-right{flex:1 1 400px;min-width:320px;}\n' +
      '.s6-title{font-weight:bold;text-align:center;margin-bottom:8px;font-size:0.88em;}\n' +
      '.s6-title-left{color:#2c5aa0;}\n' +
      '.s6-title-right{color:#155724;}\n' +
      // Field card
      '.s6-card{background:#f8f9fa;border:1px solid #dee2e6;border-radius:8px;padding:12px 14px;' +
        'font-family:Consolas,monospace;font-size:0.82em;transition:all 0.3s;}\n' +
      '.s6-card-header{font-weight:bold;color:#2c5aa0;font-size:1.05em;margin-bottom:8px;' +
        'padding-bottom:6px;border-bottom:1px solid #dee2e6;}\n' +
      '.s6-row{display:flex;margin:3px 0;}\n' +
      '.s6-lbl{color:#6c757d;min-width:100px;}\n' +
      '.s6-val{color:#212529;font-weight:bold;}\n' +
      '.s6-val-true{color:#28a745;}\n' +
      '.s6-val-false{color:#999;}\n' +
      '.s6-badge{display:inline-block;padding:1px 8px;border-radius:10px;font-size:0.85em;' +
        'font-weight:bold;margin-top:6px;}\n' +
      // Code panel
      '.s6-code{background:#1e1e2e;border:1px solid #313244;border-radius:8px;padding:14px 16px;' +
        'font-family:Consolas,monospace;font-size:0.82em;color:#cdd6f4;line-height:1.6;' +
        'overflow-x:auto;white-space:pre;min-height:80px;transition:all 0.3s;}\n' +
      '.s6-kw{color:#cba6f7;}\n' +   // keyword purple
      '.s6-fn{color:#89b4fa;}\n' +   // function blue
      '.s6-str{color:#a6e3a1;}\n' +  // string green
      '.s6-cmt{color:#6c7086;font-style:italic;}\n' + // comment gray
      '.s6-hl{color:#f9e2af;}\n' +   // highlight yellow
      '.s6-tp{color:#f38ba8;}\n' +   // type pink
      '.s6-num{color:#fab387;}\n' +   // number peach
      // Pattern label
      '.s6-pattern{display:inline-block;padding:4px 12px;border-radius:6px;font-size:0.85em;' +
        'font-weight:bold;margin-bottom:8px;}\n' +
      '</style>\n';

    // --- Field card placeholder (filled by JS) ---
    var leftPanel =
      '<div class="s6-left">' +
        '<div class="s6-title s6-title-left">FieldNode \uC18D\uC131</div>' +
        '<div class="s6-card" id="' + P + '-card">' +
          '<div class="s6-card-header" id="' + P + '-card-hdr">FieldNode</div>' +
          '<div id="' + P + '-card-body"></div>' +
        '</div>' +
      '</div>';

    // --- Code panel placeholder ---
    var rightPanel =
      '<div class="s6-right">' +
        '<div class="s6-title s6-title-right">\uC0DD\uC131\uB418\uB294 C++ \uCF54\uB4DC</div>' +
        '<div id="' + P + '-pattern" style="text-align:center;"></div>' +
        '<div class="s6-code" id="' + P + '-code"></div>' +
      '</div>';

    // --- Info box ---
    var infoBox =
      '<div id="' + ids.info + '" style="text-align:center;padding:10px 16px;background:#e8f4fd;' +
        'border-left:4px solid #5ba3d9;border-radius:0 6px 6px 0;margin-bottom:10px;' +
        'font-size:0.92em;color:#333;min-height:2.5em;display:flex;align-items:center;' +
        'justify-content:center;">\uCD08\uAE30 \uC0C1\uD0DC \u2014 8\uAC00\uC9C0 \uB9C8\uC0EC\uB9C1 \uD328\uD134\uC744 \uB2E8\uACC4\uBCC4\uB85C \uD655\uC778\uD569\uB2C8\uB2E4</div>';

    // --- Stepper controls ---
    var controls =
      '<div class="stepper-controls">' +
        '<button class="stepper-btn" id="' + ids.prev + '" disabled>\u2190 \uC774\uC804</button>' +
        '<span class="stepper-label" id="' + ids.val + '">\uB2E8\uACC4 0 / 8</span>' +
        '<button class="stepper-btn" id="' + ids.next + '">\uB2E4\uC74C \u2192</button>' +
      '</div>';

    // --- Legend ---
    function dot(bg, border, label) {
      return '<span><span style="display:inline-block;width:12px;height:12px;border-radius:2px;' +
        'background:' + bg + ';border:1px solid ' + border + ';vertical-align:middle;margin-right:3px;' +
        '"></span>' + label + '</span>';
    }
    var legend =
      '<div style="display:flex;gap:14px;flex-wrap:wrap;justify-content:center;' +
      'font-size:0.76em;color:#555;margin-top:8px;padding-top:8px;border-top:1px solid #eee;">' +
        dot('#cba6f7', '#9b7cc9', '\uD0A4\uC6CC\uB4DC') +
        dot('#89b4fa', '#5a8ad4', '\uD568\uC218/\uC811\uADFC\uC790') +
        dot('#a6e3a1', '#6ebe6a', '\uBB38\uC790\uC5F4') +
        dot('#f9e2af', '#d4b96a', '\uD558\uC774\uB77C\uC774\uD2B8') +
        dot('#f38ba8', '#c96b85', '\uD0C0\uC785') +
      '</div>';

    return style +
      '<div class="s6-wrap">' + leftPanel + rightPanel + '</div>' +
      infoBox + controls + legend;
  },

  build: function(ids) {
    // === Step definitions ===
    // Each step: field properties, pattern label, code HTML, description
    var steps = [
      {
        desc: '\uCD08\uAE30 \uC0C1\uD0DC \u2014 \uC544\uB798 \u2192 \uBC84\uD2BC\uC73C\uB85C 8\uAC00\uC9C0 \uB9C8\uC0EC\uB9C1 \uD328\uD134\uC744 \uD655\uC778\uD558\uC138\uC694.',
        field: null,
        pattern: '',
        patternColor: '',
        code: '<span class="s6-cmt">// \uB9C8\uC0EC\uB9C1 \uC0DD\uC131\uC790 \uB0B4\uBD80 \u2014 \uD544\uB4DC \uD0C0\uC785\uBCC4 \uCF54\uB4DC \uD328\uD134</span>'
      },
      {
        desc: '\uD328\uD134 1: string \u2192 UTF8_TO_TCHAR \uBCC0\uD658. protoc\uC758 std::string\uC744 Unreal FString\uC73C\uB85C \uBCC0\uD658\uD569\uB2C8\uB2E4.',
        field: { name: 'name', type: 'string', number: 1, flags: [] },
        pattern: '\uD328\uD134 1: UTF8_TO_TCHAR',
        patternColor: '#cba6f7',
        code:
          '<span class="s6-hl">Name</span> = <span class="s6-tp">FString</span>(' +
          '<span class="s6-fn">UTF8_TO_TCHAR</span>(' +
          '<span class="s6-fn">proto</span>.<span class="s6-fn">name</span>().<span class="s6-fn">c_str</span>()));'
      },
      {
        desc: '\uD328\uD134 2: \uD504\uB9AC\uBBF8\uD2F0\uBE0C \u2192 \uC9C1\uC811 \uB300\uC785. proto accessor\uC758 \uBC18\uD658\uAC12\uC744 \uADF8\uB300\uB85C \uB300\uC785\uD569\uB2C8\uB2E4.',
        field: { name: 'level', type: 'int32', number: 2, flags: [] },
        pattern: '\uD328\uD134 2: \uC9C1\uC811 \uB300\uC785',
        patternColor: '#89b4fa',
        code:
          '<span class="s6-hl">Level</span> = <span class="s6-fn">proto</span>.<span class="s6-fn">level</span>();'
      },
      {
        desc: '\uD328\uD134 3: bytes \u2192 SetNum + FMemory::Memcpy 3\uC904 \uD328\uD134. \uBC14\uC774\uD2B8 \uBC30\uC5F4\uC744 TArray<uint8>\uB85C \uBCF5\uC0AC\uD569\uB2C8\uB2E4.',
        field: { name: 'avatar', type: 'bytes', number: 4, flags: [] },
        pattern: '\uD328\uD134 3: FMemory::Memcpy',
        patternColor: '#fab387',
        code:
          '<span class="s6-kw">const</span> <span class="s6-tp">std::string</span>&amp; <span class="s6-hl">_bytes_avatar</span> = <span class="s6-fn">proto</span>.<span class="s6-fn">avatar</span>();\n' +
          '<span class="s6-hl">Avatar</span>.<span class="s6-fn">SetNum</span>(<span class="s6-hl">_bytes_avatar</span>.<span class="s6-fn">size</span>());\n' +
          '<span class="s6-tp">FMemory</span>::<span class="s6-fn">Memcpy</span>(<span class="s6-hl">Avatar</span>.<span class="s6-fn">GetData</span>(), <span class="s6-hl">_bytes_avatar</span>.<span class="s6-fn">data</span>(), <span class="s6-hl">_bytes_avatar</span>.<span class="s6-fn">size</span>());'
      },
      {
        desc: '\uD328\uD134 4: repeated \u2192 Reserve + for \uB8E8\uD504. \uBC30\uC5F4 \uD06C\uAE30\uB97C \uBBF8\uB9AC \uD655\uBCF4\uD558\uACE0 \uC21C\uD68C\uD558\uBA70 Add\uD569\uB2C8\uB2E4.',
        field: { name: 'tags', type: 'string', number: 5, flags: ['IsRepeated'] },
        pattern: '\uD328\uD134 4: Reserve + for \uB8E8\uD504',
        patternColor: '#a6e3a1',
        code:
          '<span class="s6-hl">Tags</span>.<span class="s6-fn">Reserve</span>(<span class="s6-fn">proto</span>.<span class="s6-fn">tags_size</span>());\n' +
          '<span class="s6-kw">for</span> (<span class="s6-tp">int</span> <span class="s6-hl">i</span> = <span class="s6-num">0</span>; <span class="s6-hl">i</span> &lt; <span class="s6-fn">proto</span>.<span class="s6-fn">tags_size</span>(); ++<span class="s6-hl">i</span>)\n' +
          '{\n' +
          '    <span class="s6-hl">Tags</span>.<span class="s6-fn">Add</span>(<span class="s6-tp">FString</span>(<span class="s6-fn">UTF8_TO_TCHAR</span>(<span class="s6-fn">proto</span>.<span class="s6-fn">tags</span>(<span class="s6-hl">i</span>).<span class="s6-fn">c_str</span>())));\n' +
          '}'
      },
      {
        desc: '\uD328\uD134 5: map \u2192 \uAD6C\uC870\uC801 \uBC14\uC778\uB529 range-for. C++17 [key, value] \uBC14\uC778\uB529\uC73C\uB85C \uC21C\uD68C\uD569\uB2C8\uB2E4.',
        field: { name: 'scores', type: 'map<string, int32>', number: 6, flags: ['IsMap'], mapKey: 'string', mapValue: 'int32' },
        pattern: '\uD328\uD134 5: range-for [key, value]',
        patternColor: '#f9e2af',
        code:
          '<span class="s6-kw">for</span> (<span class="s6-kw">const</span> <span class="s6-kw">auto</span>&amp; [<span class="s6-hl">key</span>, <span class="s6-hl">value</span>] : <span class="s6-fn">proto</span>.<span class="s6-fn">scores</span>())\n' +
          '{\n' +
          '    <span class="s6-hl">Scores</span>.<span class="s6-fn">Add</span>(<span class="s6-tp">FString</span>(<span class="s6-fn">UTF8_TO_TCHAR</span>(<span class="s6-hl">key</span>.<span class="s6-fn">c_str</span>())), <span class="s6-hl">value</span>);\n' +
          '}'
      },
      {
        desc: '\uD328\uD134 6: optional \u2192 has_xxx() \uAC00\uB4DC. \uC874\uC7AC \uC5EC\uBD80 \uD655\uC778 \uD6C4 TOptional\uC5D0 \uB300\uC785\uD569\uB2C8\uB2E4.',
        field: { name: 'rating', type: 'float', number: 7, flags: ['IsOptional'] },
        pattern: '\uD328\uD134 6: has_xxx() \uAC00\uB4DC',
        patternColor: '#94e2d5',
        code:
          '<span class="s6-kw">if</span> (<span class="s6-fn">proto</span>.<span class="s6-fn">has_rating</span>())\n' +
          '{\n' +
          '    <span class="s6-hl">Rating</span> = <span class="s6-fn">proto</span>.<span class="s6-fn">rating</span>();\n' +
          '}'
      },
      {
        desc: '\uD328\uD134 7: enum \u2192 static_cast. protoc\uC758 \uC815\uC218\uAC12\uC744 Unreal UENUM \uD0C0\uC785\uC73C\uB85C \uCE90\uC2A4\uD305\uD569\uB2C8\uB2E4.',
        field: { name: 'state', type: 'PlayerState', number: 8, flags: ['IsEnum'] },
        pattern: '\uD328\uD134 7: static_cast',
        patternColor: '#f38ba8',
        code:
          '<span class="s6-hl">State</span> = <span class="s6-kw">static_cast</span>&lt;<span class="s6-tp">EPlayerStateProto</span>&gt;(<span class="s6-fn">proto</span>.<span class="s6-fn">state</span>());'
      },
      {
        desc: '\uD328\uD134 8: \uBA54\uC2DC\uC9C0 \u2192 \uC7AC\uADC0 \uB9C8\uC0EC\uB9C1 \uC0DD\uC131\uC790. \uD574\uB2F9 \uD0C0\uC785\uC758 FTypeProto \uC0DD\uC131\uC790\uB97C \uD638\uCD9C\uD569\uB2C8\uB2E4.',
        field: { name: 'stats', type: 'PlayerStats', number: 9, flags: ['IsMessage'] },
        pattern: '\uD328\uD134 8: \uC7AC\uADC0 \uC0DD\uC131\uC790',
        patternColor: '#89dceb',
        code:
          '<span class="s6-hl">Stats</span> = <span class="s6-tp">FPlayerStatsProto</span>(<span class="s6-fn">proto</span>.<span class="s6-fn">stats</span>());'
      }
    ];

    var cfg = {
      P: ids.canvas,
      info: ids.info,
      val: ids.val,
      prev: ids.prev,
      next: ids.next,
      steps: steps
    };

    function runtime(o) {
      var P = o.P, steps = o.steps;
      var cardEl = document.getElementById(P + '-card');
      var hdrEl = document.getElementById(P + '-card-hdr');
      var bodyEl = document.getElementById(P + '-card-body');
      var codeEl = document.getElementById(P + '-code');
      var patternEl = document.getElementById(P + '-pattern');

      function buildCard(field) {
        if (!field) {
          hdrEl.textContent = 'FieldNode';
          bodyEl.innerHTML = '<div style="color:#999;padding:8px 0;text-align:center;font-style:italic;">' +
            '\u2192 \uBC84\uD2BC\uC744 \uB20C\uB7EC \uC2DC\uC791\uD558\uC138\uC694</div>';
          cardEl.style.borderColor = '#dee2e6';
          return;
        }

        var rows = '';
        function row(l, v, cls) {
          rows += '<div class="s6-row"><span class="s6-lbl">' + l + '</span>' +
            '<span class="s6-val' + (cls ? ' ' + cls : '') + '">' + v + '</span></div>';
        }

        hdrEl.textContent = 'FieldNode \u2014 "' + field.name + '"';
        row('Name:', field.name);
        row('Type:', field.type);
        row('Number:', field.number);

        var flagNames = ['IsRepeated', 'IsMap', 'IsOptional', 'IsOneOf', 'IsEnum', 'IsMessage'];
        for (var i = 0; i < flagNames.length; i++) {
          var fn = flagNames[i];
          var on = field.flags.indexOf(fn) !== -1;
          row(fn + ':', on ? 'true' : 'false', on ? 's6-val-true' : 's6-val-false');
        }

        if (field.mapKey) {
          row('MapKeyType:', field.mapKey);
          row('MapValueType:', field.mapValue);
        }

        bodyEl.innerHTML = rows;

        // Badge color on card
        cardEl.style.borderColor = '#2c5aa0';
      }

      function apply(idx) {
        var s = steps[idx];
        buildCard(s.field);
        codeEl.innerHTML = s.code;

        if (s.pattern) {
          patternEl.innerHTML = '<span class="s6-pattern" style="background:' +
            s.patternColor + '22;color:' + s.patternColor + ';border:1px solid ' +
            s.patternColor + ';">' + s.pattern + '</span>';
        } else {
          patternEl.innerHTML = '';
        }

        document.getElementById(o.info).textContent = s.desc;
        document.getElementById(o.val).textContent = '\uB2E8\uACC4 ' + idx + ' / ' + (steps.length - 1);
        document.getElementById(o.prev).disabled = idx === 0;
        document.getElementById(o.next).disabled = idx === steps.length - 1;
      }

      var cur = 0;
      document.getElementById(o.prev).onclick = function() { if (cur > 0) { cur--; apply(cur); } };
      document.getElementById(o.next).onclick = function() { if (cur < steps.length - 1) { cur++; apply(cur); } };
      apply(0);
    }

    return '(' + runtime.toString() + ')(' + JSON.stringify(cfg) + ');';
  }
};

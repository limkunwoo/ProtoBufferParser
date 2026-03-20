// cst_ast_stepper.js — Canvas2D scene plugin for Section 5
// CST → AST 변환 과정을 단계별로 보여주는 인터랙티브 스테퍼
// Type: canvas2d | Section 5 of CST_to_AST_설명
// Usage: loaded by build_qna_html.js via loadScene('cst_ast_stepper')

module.exports = {
  type: 'canvas2d',

  /**
   * Returns the HTML skeleton for the stepper visualization.
   * Two-column layout: CST (left) + AST (right), with controls below.
   */
  html: function(ids) {
    var P = ids.canvas;

    // CST nodes: [localId, displayText]
    // Simplified from full 29-node CST — collapses wrapper rules
    // (messageName→ident, fieldName→ident, fieldNumber→intLit, messageElement→field)
    var cst = [
      ['c0',  'messageDef'],
      ['c1',  ' \u251C\u2500 MESSAGE'],
      ['c2',  ' \u251C\u2500 messageName'],
      ['c3',  ' \u2502   \u2514\u2500 ident'],
      ['c4',  ' \u2502       \u2514\u2500 ID("Player")'],
      ['c5',  ' \u2514\u2500 messageBody'],
      ['c6',  '     \u251C\u2500 LC'],
      ['c7',  '     \u251C\u2500 field[0]'],
      ['c8',  '     \u2502   \u251C\u2500 type_ \u2192 STRING'],
      ['c9',  '     \u2502   \u251C\u2500 fieldName \u2192 ID("name")'],
      ['c10', '     \u2502   \u251C\u2500 EQ'],
      ['c11', '     \u2502   \u251C\u2500 fieldNumber \u2192 INT_LIT("1")'],
      ['c12', '     \u2502   \u2514\u2500 SEMI'],
      ['c13', '     \u251C\u2500 field[1]'],
      ['c14', '     \u2502   \u251C\u2500 type_ \u2192 INT32'],
      ['c15', '     \u2502   \u251C\u2500 fieldName \u2192 ID("level")'],
      ['c16', '     \u2502   \u251C\u2500 EQ'],
      ['c17', '     \u2502   \u251C\u2500 fieldNumber \u2192 INT_LIT("2")'],
      ['c18', '     \u2502   \u2514\u2500 SEMI'],
      ['c19', '     \u2514\u2500 RC']
    ];

    // AST nodes (initially hidden, revealed step by step)
    var ast = [
      ['a0',  'MessageNode'],
      ['a1',  ' \u251C\u2500 name: "Player"'],
      ['a2',  ' \u2514\u2500 fields'],
      ['a3',  '     \u251C\u2500 FieldNode'],
      ['a4',  '     \u2502   \u251C\u2500 type: "string"'],
      ['a5',  '     \u2502   \u251C\u2500 name: "name"'],
      ['a6',  '     \u2502   \u2514\u2500 number: 1'],
      ['a7',  '     \u2514\u2500 FieldNode'],
      ['a8',  '         \u251C\u2500 type: "int32"'],
      ['a9',  '         \u251C\u2500 name: "level"'],
      ['a10', '         \u2514\u2500 number: 2']
    ];

    // Shared inline style for each node line
    var ns = 'padding:2px 6px;border-radius:3px;margin:1px 0;' +
      'transition:background 0.3s,color 0.3s,opacity 0.3s;white-space:pre;';

    var cstHtml = cst.map(function(n) {
      return '<div id="' + P + '-' + n[0] + '" style="' + ns + '">' + n[1] + '</div>';
    }).join('\n');

    var astHtml = ast.map(function(n) {
      return '<div id="' + P + '-' + n[0] + '" style="' + ns +
        'opacity:0;max-height:0;overflow:hidden;transition:all 0.4s;">' + n[1] + '</div>';
    }).join('\n');

    // Color legend
    var legend =
      '<div style="display:flex;gap:12px;flex-wrap:wrap;justify-content:center;' +
      'font-size:0.78em;color:#555;margin-top:8px;padding-top:8px;border-top:1px solid #eee;">' +
        '<span><span style="display:inline-block;width:12px;height:12px;border-radius:2px;' +
        'background:#cce5ff;border:1px solid #004085;vertical-align:middle;margin-right:3px;"></span>' +
        '\uBC29\uBB38 \uC911</span>' +     // 방문 중
        '<span><span style="display:inline-block;width:12px;height:12px;border-radius:2px;' +
        'background:#d4edda;border:1px solid #155724;vertical-align:middle;margin-right:3px;"></span>' +
        '\uAC12 \uCD94\uCD9C</span>' +     // 값 추출
        '<span><span style="display:inline-block;width:12px;height:12px;border-radius:2px;' +
        'background:#f0f0f0;border:1px solid #ccc;vertical-align:middle;margin-right:3px;"></span>' +
        '\uCC98\uB9AC \uC644\uB8CC</span>' + // 처리 완료
        '<span><span style="display:inline-block;width:12px;height:12px;border-radius:2px;' +
        'background:#fff;border:1px solid #ddd;vertical-align:middle;margin-right:3px;opacity:0.4;"></span>' +
        '\uBB34\uC2DC/\uD3D0\uAE30</span>' + // 무시/폐기
      '</div>';

    return '' +
      // Two-column layout
      '<div style="display:flex;gap:16px;flex-wrap:wrap;justify-content:center;margin-bottom:12px;">' +
        // CST column
        '<div style="flex:1 1 420px;min-width:320px;max-width:100%;">' +
          '<div style="font-weight:bold;text-align:center;color:#2c5aa0;margin-bottom:6px;' +
          'font-size:0.9em;">CST (Parse Tree) \u2014 20\uAC1C \uB178\uB4DC</div>' +
          '<div style="background:#f8f9fa;border:1px solid #e0e0e0;border-radius:6px;' +
          'padding:8px 10px;font-family:Consolas,\'Courier New\',monospace;' +
          'font-size:0.76em;line-height:1.45;">' +
            cstHtml +
          '</div>' +
        '</div>' +
        // AST column
        '<div style="flex:1 1 300px;min-width:250px;max-width:100%;">' +
          '<div style="font-weight:bold;text-align:center;color:#8b1a1a;margin-bottom:6px;' +
          'font-size:0.9em;">AST \u2014 3\uAC1C \uB178\uB4DC</div>' +
          '<div id="' + P + '-ast-box" style="background:#fdf8f8;border:1px solid #e0d0d0;' +
          'border-radius:6px;padding:8px 10px;font-family:Consolas,\'Courier New\',monospace;' +
          'font-size:0.8em;line-height:1.45;min-height:120px;">' +
            astHtml +
          '</div>' +
        '</div>' +
      '</div>' +
      // Step description box
      '<div id="' + ids.info + '" style="text-align:center;padding:10px 16px;background:#e8f4fd;' +
        'border-left:4px solid #5ba3d9;border-radius:0 6px 6px 0;margin-bottom:10px;' +
        'font-size:0.92em;color:#333;min-height:2.5em;display:flex;align-items:center;' +
        'justify-content:center;">' +
        '\uCD08\uAE30 \uC0C1\uD0DC \u2014 CST \uC804\uCCB4 \uAD6C\uC870\uAC00 \uD45C\uC2DC\uB429\uB2C8\uB2E4' +
      '</div>' +
      // Stepper controls
      '<div class="stepper-controls">' +
        '<button class="stepper-btn" id="' + ids.prev + '" disabled>\u2190 \uC774\uC804</button>' +
        '<span class="stepper-label" id="' + ids.val + '">\uB2E8\uACC4 0 / 8</span>' +
        '<button class="stepper-btn" id="' + ids.next + '">\uB2E4\uC74C \u2192</button>' +
      '</div>' +
      legend;
  },

  /**
   * Returns JS code for the stepper logic.
   * Defines step states and handles prev/next navigation.
   */
  build: function(ids) {
    var cstIds = ['c0','c1','c2','c3','c4','c5','c6','c7','c8','c9',
                  'c10','c11','c12','c13','c14','c15','c16','c17','c18','c19'];
    var astIds = ['a0','a1','a2','a3','a4','a5','a6','a7','a8','a9','a10'];

    // Style keys: N=Normal, A=Active, E=Extract, F=Fade, D=Done, S=Show, G=Glow
    var styles = {
      N: { bg: 'transparent', c: '#333',    o: '1'   },
      A: { bg: '#cce5ff',     c: '#004085', o: '1'   },
      E: { bg: '#d4edda',     c: '#155724', o: '1'   },
      F: { bg: 'transparent', c: '#ccc',    o: '0.4' },
      D: { bg: '#f0f0f0',     c: '#888',    o: '0.7' },
      S: { bg: '#fde8e8',     c: '#8b1a1a', o: '1'   },
      G: { bg: '#d4edda',     c: '#155724', o: '1'   }
    };

    // Steps: { d: description, c: {cstId: styleKey}, a: {astId: styleKey} }
    // CST ids not listed default to 'N' (Normal)
    // AST ids not listed default to hidden
    var steps = [
      {
        d: '\uCD08\uAE30 \uC0C1\uD0DC \u2014 CST(Parse Tree) \uC804\uCCB4 \uAD6C\uC870\uAC00 \uD45C\uC2DC\uB429\uB2C8\uB2E4.',
        c: {},
        a: {}
      },
      {
        d: 'VisitMessageDef(ctx) \uD638\uCD9C \u2014 \uCD5C\uC0C1\uC704 \uADDC\uCE59 \uB178\uB4DC \uBC29\uBB38\uC744 \uC2DC\uC791\uD569\uB2C8\uB2E4.',
        c: { c0: 'A' },
        a: {}
      },
      {
        d: 'ctx.messageName().GetText() \u2192 "Player" \uCD94\uCD9C. \uB798\uD37C \uADDC\uCE59(messageName \u2192 ident)\uC744 \uD1B5\uACFC\uD558\uC5EC \uAC12\uC744 \uC5BB\uC2B5\uB2C8\uB2E4.',
        c: { c0: 'A', c1: 'F', c2: 'E', c3: 'E', c4: 'E' },
        a: { a0: 'G', a1: 'G' }
      },
      {
        d: 'messageBody \uC9C4\uC785. LC({)\uC640 RC(}) \uAD6C\uB450\uC810 \uD1A0\uD070\uC740 \uAD6C\uC870 \uC815\uBCF4\uAC00 \uC5C6\uC73C\uBBC0\uB85C \uBB34\uC2DC\uD569\uB2C8\uB2E4.',
        c: { c0: 'A', c1: 'F', c2: 'D', c3: 'D', c4: 'D', c5: 'A', c6: 'F', c19: 'F' },
        a: { a0: 'S', a1: 'S' }
      },
      {
        d: 'VisitField(ctx) \u2014 \uCCAB \uBC88\uC9F8 field \uADDC\uCE59 \uB178\uB4DC\uB97C \uBC29\uBB38\uD569\uB2C8\uB2E4.',
        c: { c0: 'D', c1: 'F', c2: 'D', c3: 'D', c4: 'D', c5: 'D', c6: 'F', c7: 'A', c19: 'F' },
        a: { a0: 'S', a1: 'S' }
      },
      {
        d: 'type_, fieldName, fieldNumber\uC5D0\uC11C \uAC12\uC744 \uCD94\uCD9C\uD569\uB2C8\uB2E4. EQ(=)\uC640 SEMI(;)\uB294 \uC758\uBBF8 \uC815\uBCF4\uAC00 \uC5C6\uC73C\uBBC0\uB85C \uD3D0\uAE30\uD569\uB2C8\uB2E4.',
        c: { c0: 'D', c1: 'F', c2: 'D', c3: 'D', c4: 'D', c5: 'D', c6: 'F',
             c7: 'A', c8: 'E', c9: 'E', c10: 'F', c11: 'E', c12: 'F', c19: 'F' },
        a: { a0: 'S', a1: 'S', a2: 'S', a3: 'G', a4: 'G', a5: 'G', a6: 'G' }
      },
      {
        d: 'VisitField(ctx) \u2014 \uB450 \uBC88\uC9F8 field \uADDC\uCE59 \uB178\uB4DC\uB97C \uBC29\uBB38\uD569\uB2C8\uB2E4.',
        c: { c0: 'D', c1: 'F', c2: 'D', c3: 'D', c4: 'D', c5: 'D', c6: 'F',
             c7: 'D', c8: 'D', c9: 'D', c10: 'F', c11: 'D', c12: 'F',
             c13: 'A', c19: 'F' },
        a: { a0: 'S', a1: 'S', a2: 'S', a3: 'S', a4: 'S', a5: 'S', a6: 'S' }
      },
      {
        d: '\uAC19\uC740 \uBC29\uC2DD\uC73C\uB85C type, name, number\uB97C \uCD94\uCD9C\uD558\uACE0 EQ, SEMI\uB97C \uD3D0\uAE30\uD569\uB2C8\uB2E4.',
        c: { c0: 'D', c1: 'F', c2: 'D', c3: 'D', c4: 'D', c5: 'D', c6: 'F',
             c7: 'D', c8: 'D', c9: 'D', c10: 'F', c11: 'D', c12: 'F',
             c13: 'A', c14: 'E', c15: 'E', c16: 'F', c17: 'E', c18: 'F', c19: 'F' },
        a: { a0: 'S', a1: 'S', a2: 'S', a3: 'S', a4: 'S', a5: 'S', a6: 'S',
             a7: 'G', a8: 'G', a9: 'G', a10: 'G' }
      },
      {
        d: '\uBCC0\uD658 \uC644\uB8CC \u2014 CST\uC758 20\uAC1C \uB178\uB4DC\uAC00 AST\uC758 3\uAC1C \uC758\uBBF8 \uB178\uB4DC(MessageNode + FieldNode\u00D72)\uB85C \uC555\uCD95\uB418\uC5C8\uC2B5\uB2C8\uB2E4.',
        c: { c0: 'D', c1: 'F', c2: 'D', c3: 'D', c4: 'D', c5: 'D', c6: 'F',
             c7: 'D', c8: 'D', c9: 'D', c10: 'F', c11: 'D', c12: 'F',
             c13: 'D', c14: 'D', c15: 'D', c16: 'F', c17: 'D', c18: 'F', c19: 'F' },
        a: { a0: 'G', a1: 'G', a2: 'G', a3: 'G', a4: 'G', a5: 'G', a6: 'G',
             a7: 'G', a8: 'G', a9: 'G', a10: 'G' }
      }
    ];

    // Build JS code as a string
    return 'var P=' + JSON.stringify(ids.canvas) + ';\n' +
      'var CI=' + JSON.stringify(cstIds) + ';\n' +
      'var AI=' + JSON.stringify(astIds) + ';\n' +
      'var S=' + JSON.stringify(styles) + ';\n' +
      'var steps=' + JSON.stringify(steps) + ';\n' +
      '\n' +
      'function el(id){return document.getElementById(P+"-"+id);}\n' +
      '\n' +
      'function ss(id,k){\n' +
      '  var e=el(id);if(!e)return;\n' +
      '  var st=S[k];\n' +
      '  e.style.background=st.bg;\n' +
      '  e.style.color=st.c;\n' +
      '  e.style.opacity=st.o;\n' +
      '  e.style.maxHeight="28px";\n' +
      '  e.style.overflow="visible";\n' +
      '}\n' +
      '\n' +
      'function sa(id,k){\n' +
      '  var e=el(id);if(!e)return;\n' +
      '  if(!k){\n' +
      '    e.style.opacity="0";\n' +
      '    e.style.maxHeight="0";\n' +
      '    e.style.overflow="hidden";\n' +
      '    return;\n' +
      '  }\n' +
      '  var st=S[k];\n' +
      '  e.style.background=st.bg;\n' +
      '  e.style.color=st.c;\n' +
      '  e.style.opacity=st.o;\n' +
      '  e.style.maxHeight="28px";\n' +
      '  e.style.overflow="visible";\n' +
      '}\n' +
      '\n' +
      'var cur=0;\n' +
      '\n' +
      'function apply(idx){\n' +
      '  var step=steps[idx];\n' +
      '  CI.forEach(function(id){ss(id,step.c[id]||"N");});\n' +
      '  AI.forEach(function(id){sa(id,step.a[id]||null);});\n' +
      '  document.getElementById(' + JSON.stringify(ids.info) + ').textContent=step.d;\n' +
      '  document.getElementById(' + JSON.stringify(ids.val) +
        ').textContent="\\uB2E8\\uACC4 "+idx+" / "+(steps.length-1);\n' +
      '  document.getElementById(' + JSON.stringify(ids.prev) + ').disabled=idx===0;\n' +
      '  document.getElementById(' + JSON.stringify(ids.next) + ').disabled=idx===steps.length-1;\n' +
      '}\n' +
      '\n' +
      'document.getElementById(' + JSON.stringify(ids.prev) + ').onclick=function(){\n' +
      '  if(cur>0){cur--;apply(cur);}\n' +
      '};\n' +
      'document.getElementById(' + JSON.stringify(ids.next) + ').onclick=function(){\n' +
      '  if(cur<steps.length-1){cur++;apply(cur);}\n' +
      '};\n' +
      '\n' +
      'apply(0);\n';
  }
};

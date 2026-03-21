// cst_ast_stepper.js — Canvas2D scene plugin for Section 5
// CST → AST 변환 과정을 단계별로 보여주는 인터랙티브 스테퍼
// 두 가지 렌더링 방식 제공: D3.js 트리 (SVG) / CSS 트리 (HTML)
// Type: canvas2d | Section 5 of CST_to_AST_설명

module.exports = {
  type: 'canvas2d',

  html: function(ids) {
    var P = ids.canvas;

    // --- CSS tree helper ---
    function li(id, label, ch) {
      var inner = ch ? '<ul>' + ch.join('') + '</ul>' : '';
      return '<li><span class="s5-node" id="' + P + '-css-' + id + '">' +
        label + '</span>' + inner + '</li>';
    }

    var cstTree = '<ul class="s5-tree">' + li('c0', 'messageDef', [
      li('c1', 'MESSAGE'),
      li('c2', 'messageName', [li('c3', 'ident', [li('c4', 'ID("Player")')])]),
      li('c5', 'messageBody', [
        li('c6', 'LC'),
        li('c7', 'field[0]', [
          li('c8', 'type_: STRING'), li('c9', 'fieldName: ID("name")'),
          li('c10', 'EQ'), li('c11', 'fieldNumber: INT_LIT("1")'), li('c12', 'SEMI')
        ]),
        li('c13', 'field[1]', [
          li('c14', 'type_: INT32'), li('c15', 'fieldName: ID("level")'),
          li('c16', 'EQ'), li('c17', 'fieldNumber: INT_LIT("2")'), li('c18', 'SEMI')
        ]),
        li('c19', 'RC')
      ])
    ]) + '</ul>';

    var astTree = '<ul class="s5-tree">' + li('a0', 'MessageNode', [
      li('a1', 'name: "Player"'),
      li('a2', 'fields', [
        li('a3', 'FieldNode', [
          li('a4', 'type: "string"'), li('a5', 'name: "name"'), li('a6', 'number: 1')
        ]),
        li('a7', 'FieldNode', [
          li('a8', 'type: "int32"'), li('a9', 'name: "level"'), li('a10', 'number: 2')
        ])
      ])
    ]) + '</ul>';

    // --- Scoped styles ---
    var style = '<style>\n' +
      '.s5-tabs{display:flex;gap:0;margin-bottom:0;}\n' +
      '.s5-tab{padding:8px 20px;border:1px solid #ddd;border-bottom:none;border-radius:6px 6px 0 0;' +
        'background:#f5f5f5;color:#666;cursor:pointer;font-size:0.88em;font-weight:bold;transition:all 0.2s;}\n' +
      '.s5-tab.active{background:#fff;color:#2c5aa0;border-color:#ccc;position:relative;' +
        'z-index:1;margin-bottom:-1px;padding-bottom:9px;}\n' +
      '.s5-tab:not(.active):hover{background:#eef3fa;color:#2c5aa0;}\n' +
      '.s5-view{border:1px solid #ccc;border-radius:0 6px 6px 6px;padding:16px;background:#fff;margin-bottom:12px;}\n' +
      '.s5-tree,.s5-tree ul{list-style:none;margin:0;padding-left:0;}\n' +
      '.s5-tree ul{padding-left:22px;}\n' +
      '.s5-tree li{position:relative;padding:1px 0;}\n' +
      ".s5-tree li::before{content:'';position:absolute;left:-14px;top:0;height:100%;border-left:1.5px solid #bbb;}\n" +
      ".s5-tree li::after{content:'';position:absolute;left:-14px;top:14px;width:12px;border-top:1.5px solid #bbb;}\n" +
      '.s5-tree li:last-child::before{height:14px;}\n' +
      '.s5-tree>li::before,.s5-tree>li::after{display:none;}\n' +
      ".s5-node{display:inline-block;padding:2px 8px;border-radius:4px;font-family:Consolas,'Courier New',monospace;" +
        'font-size:0.78em;transition:background 0.3s,color 0.3s,opacity 0.3s;}\n' +
      '.s5-d3-box{border:1px solid #e0e0e0;border-radius:6px;overflow:hidden;min-height:120px;}\n' +
      '.s5-d3-box svg{display:block;width:100%;height:auto;min-height:300px;max-height:700px;cursor:grab;}\n' +
      '.s5-css-ast{background:#fdf8f8;border:1px solid #e0d0d0;border-radius:6px;padding:8px 10px;min-height:120px;}\n' +
      '</style>\n';

    // --- Layout constants ---
    var cTitleS = 'font-weight:bold;text-align:center;color:#2c5aa0;margin-bottom:6px;font-size:0.9em;';
    var aTitleS = 'font-weight:bold;text-align:center;color:#8b1a1a;margin-bottom:6px;font-size:0.9em;';
    var twoCol = 'display:flex;gap:16px;flex-wrap:wrap;justify-content:center;';
    var cCol = 'flex:1 1 420px;min-width:300px;max-width:100%;';
    var aCol = 'flex:1 1 300px;min-width:250px;max-width:100%;';

    // --- Tabs ---
    var tabsHtml =
      '<div class="s5-tabs">' +
        '<button class="s5-tab active" id="' + P + '-tab-d3">D3.js \uD2B8\uB9AC (SVG)</button>' +
        '<button class="s5-tab" id="' + P + '-tab-css">CSS \uD2B8\uB9AC (HTML)</button>' +
      '</div>';

    // --- D3 view (empty containers, filled by build()) ---
    var d3View =
      '<div class="s5-view" id="' + P + '-view-d3">' +
        '<div style="' + twoCol + '">' +
          '<div style="' + cCol + '">' +
            '<div style="' + cTitleS + '">CST (Parse Tree) \u2014 20\uAC1C \uB178\uB4DC</div>' +
            '<div class="s5-d3-box" id="' + P + '-d3-cst"></div>' +
          '</div>' +
          '<div style="' + aCol + '">' +
            '<div style="' + aTitleS + '">AST \u2014 3\uAC1C \uB178\uB4DC</div>' +
            '<div class="s5-d3-box" id="' + P + '-d3-ast"></div>' +
          '</div>' +
        '</div>' +
      '</div>';

    // --- CSS tree view (hidden initially) ---
    var cssView =
      '<div class="s5-view" id="' + P + '-view-css" style="display:none;">' +
        '<div style="' + twoCol + '">' +
          '<div style="' + cCol + 'text-align:left;">' +
            '<div style="' + cTitleS + '">CST (Parse Tree) \u2014 20\uAC1C \uB178\uB4DC</div>' +
            '<div style="background:#f8f9fa;border:1px solid #e0e0e0;border-radius:6px;padding:8px 12px;">' +
              cstTree + '</div>' +
          '</div>' +
          '<div style="' + aCol + 'text-align:left;">' +
            '<div style="' + aTitleS + '">AST \u2014 3\uAC1C \uB178\uB4DC</div>' +
            '<div class="s5-css-ast" id="' + P + '-css-ast-box">' + astTree + '</div>' +
          '</div>' +
        '</div>' +
      '</div>';

    // --- Info box ---
    var infoBox =
      '<div id="' + ids.info + '" style="text-align:center;padding:10px 16px;background:#e8f4fd;' +
        'border-left:4px solid #5ba3d9;border-radius:0 6px 6px 0;margin-bottom:10px;' +
        'font-size:0.92em;color:#333;min-height:2.5em;display:flex;align-items:center;' +
        'justify-content:center;">\uCD08\uAE30 \uC0C1\uD0DC</div>';

    // --- Stepper controls ---
    var controls =
      '<div class="stepper-controls">' +
        '<button class="stepper-btn" id="' + ids.prev + '" disabled>\u2190 \uC774\uC804</button>' +
        '<span class="stepper-label" id="' + ids.val + '">\uB2E8\uACC4 0 / 8</span>' +
        '<button class="stepper-btn" id="' + ids.next + '">\uB2E4\uC74C \u2192</button>' +
      '</div>';

    // --- Legend ---
    function dot(bg, border, label, extra) {
      return '<span><span style="display:inline-block;width:12px;height:12px;border-radius:2px;' +
        'background:' + bg + ';border:1px solid ' + border + ';vertical-align:middle;margin-right:3px;' +
        (extra || '') + '"></span>' + label + '</span>';
    }
    var legend =
      '<div style="display:flex;gap:12px;flex-wrap:wrap;justify-content:center;' +
      'font-size:0.78em;color:#555;margin-top:8px;padding-top:8px;border-top:1px solid #eee;">' +
        dot('#cce5ff', '#004085', '\uBC29\uBB38 \uC911') +
        dot('#d4edda', '#155724', '\uAC12 \uCD94\uCD9C') +
        dot('#f0f0f0', '#ccc', '\uCC98\uB9AC \uC644\uB8CC') +
        dot('#fff', '#ddd', '\uBB34\uC2DC/\uD3D0\uAE30', 'opacity:0.4;') +
      '</div>';

    return style + tabsHtml + d3View + cssView + infoBox + controls + legend;
  },

  build: function(ids) {
    var cstIds = ['c0','c1','c2','c3','c4','c5','c6','c7','c8','c9',
                  'c10','c11','c12','c13','c14','c15','c16','c17','c18','c19'];
    var astIds = ['a0','a1','a2','a3','a4','a5','a6','a7','a8','a9','a10'];

    var styles = {
      N: { bg: 'transparent', c: '#333',    o: 1   },
      A: { bg: '#cce5ff',     c: '#004085', o: 1   },
      E: { bg: '#d4edda',     c: '#155724', o: 1   },
      F: { bg: 'transparent', c: '#ccc',    o: 0.4 },
      D: { bg: '#f0f0f0',     c: '#888',    o: 0.7 },
      S: { bg: '#fde8e8',     c: '#8b1a1a', o: 1   },
      G: { bg: '#d4edda',     c: '#155724', o: 1   }
    };

    var steps = [
      { d: '\uCD08\uAE30 \uC0C1\uD0DC \u2014 CST(Parse Tree) \uC804\uCCB4 \uAD6C\uC870\uAC00 \uD45C\uC2DC\uB429\uB2C8\uB2E4.', c: {}, a: {} },
      { d: 'VisitMessageDef(ctx) \uD638\uCD9C \u2014 \uCD5C\uC0C1\uC704 \uADDC\uCE59 \uB178\uB4DC \uBC29\uBB38 \uC2DC\uC791.', c: { c0:'A' }, a: {} },
      { d: 'ctx.messageName().GetText() \u2192 "Player" \uCD94\uCD9C.', c: { c0:'A', c1:'F', c2:'E', c3:'E', c4:'E' }, a: { a0:'G', a1:'G' } },
      { d: 'messageBody \uC9C4\uC785. LC, RC \uAD6C\uB450\uC810 \uD1A0\uD070 \uBB34\uC2DC.', c: { c0:'A', c1:'F', c2:'D', c3:'D', c4:'D', c5:'A', c6:'F', c19:'F' }, a: { a0:'S', a1:'S' } },
      { d: 'VisitField \u2014 \uCCAB \uBC88\uC9F8 field \uBC29\uBB38.', c: { c0:'D', c1:'F', c2:'D', c3:'D', c4:'D', c5:'D', c6:'F', c7:'A', c19:'F' }, a: { a0:'S', a1:'S' } },
      { d: 'type_, fieldName, fieldNumber \uAC12 \uCD94\uCD9C. EQ, SEMI \uD3D0\uAE30.', c: { c0:'D', c1:'F', c2:'D', c3:'D', c4:'D', c5:'D', c6:'F', c7:'A', c8:'E', c9:'E', c10:'F', c11:'E', c12:'F', c19:'F' }, a: { a0:'S', a1:'S', a2:'S', a3:'G', a4:'G', a5:'G', a6:'G' } },
      { d: 'VisitField \u2014 \uB450 \uBC88\uC9F8 field \uBC29\uBB38.', c: { c0:'D', c1:'F', c2:'D', c3:'D', c4:'D', c5:'D', c6:'F', c7:'D', c8:'D', c9:'D', c10:'F', c11:'D', c12:'F', c13:'A', c19:'F' }, a: { a0:'S', a1:'S', a2:'S', a3:'S', a4:'S', a5:'S', a6:'S' } },
      { d: 'type, name, number \uCD94\uCD9C, EQ/SEMI \uD3D0\uAE30.', c: { c0:'D', c1:'F', c2:'D', c3:'D', c4:'D', c5:'D', c6:'F', c7:'D', c8:'D', c9:'D', c10:'F', c11:'D', c12:'F', c13:'A', c14:'E', c15:'E', c16:'F', c17:'E', c18:'F', c19:'F' }, a: { a0:'S', a1:'S', a2:'S', a3:'S', a4:'S', a5:'S', a6:'S', a7:'G', a8:'G', a9:'G', a10:'G' } },
      { d: '\uBCC0\uD658 \uC644\uB8CC \u2014 20\uAC1C CST \uB178\uB4DC \u2192 3\uAC1C AST \uC758\uBBF8 \uB178\uB4DC\uB85C \uC555\uCD95.', c: { c0:'D', c1:'F', c2:'D', c3:'D', c4:'D', c5:'D', c6:'F', c7:'D', c8:'D', c9:'D', c10:'F', c11:'D', c12:'F', c13:'D', c14:'D', c15:'D', c16:'F', c17:'D', c18:'F', c19:'F' }, a: { a0:'G', a1:'G', a2:'G', a3:'G', a4:'G', a5:'G', a6:'G', a7:'G', a8:'G', a9:'G', a10:'G' } }
    ];

    var cstData = {
      id:'c0', name:'messageDef', children:[
        {id:'c1', name:'MESSAGE'},
        {id:'c2', name:'messageName', children:[
          {id:'c3', name:'ident', children:[
            {id:'c4', name:'ID("Player")'}
          ]}
        ]},
        {id:'c5', name:'messageBody', children:[
          {id:'c6', name:'LC'},
          {id:'c7', name:'field[0]', children:[
            {id:'c8', name:'type_: STRING'},
            {id:'c9', name:'fieldName: ID("name")'},
            {id:'c10', name:'EQ'},
            {id:'c11', name:'fieldNumber: INT_LIT("1")'},
            {id:'c12', name:'SEMI'}
          ]},
          {id:'c13', name:'field[1]', children:[
            {id:'c14', name:'type_: INT32'},
            {id:'c15', name:'fieldName: ID("level")'},
            {id:'c16', name:'EQ'},
            {id:'c17', name:'fieldNumber: INT_LIT("2")'},
            {id:'c18', name:'SEMI'}
          ]},
          {id:'c19', name:'RC'}
        ]}
      ]
    };

    var astData = {
      id:'a0', name:'MessageNode', children:[
        {id:'a1', name:'name: "Player"'},
        {id:'a2', name:'fields', children:[
          {id:'a3', name:'FieldNode', children:[
            {id:'a4', name:'type: "string"'},
            {id:'a5', name:'name: "name"'},
            {id:'a6', name:'number: 1'}
          ]},
          {id:'a7', name:'FieldNode', children:[
            {id:'a8', name:'type: "int32"'},
            {id:'a9', name:'name: "level"'},
            {id:'a10', name:'number: 2'}
          ]}
        ]}
      ]
    };

    var cfg = {
      P: ids.canvas,
      info: ids.info,
      val: ids.val,
      prev: ids.prev,
      next: ids.next,
      S: styles,
      steps: steps,
      CI: cstIds,
      AI: astIds,
      cstData: cstData,
      astData: astData
    };

    // Runtime function — serialized via toString(), executed in browser
    function runtime(o) {
      var P=o.P, S=o.S, steps=o.steps, CI=o.CI, AI=o.AI;

      // ===== D3.js Horizontal Tree =====
      function textWidth(l) { return l.length * 8.4 + 24; }

      function renderD3Tree(containerId, treeData, isAST) {
        var container = document.getElementById(containerId);
        var root = d3.hierarchy(treeData);
        var layout = d3.tree().nodeSize([38, 200])
          .separation(function(a, b) { return a.parent === b.parent ? 1 : 1.3; });
        layout(root);
        /* Swap x/y for horizontal layout */
        root.each(function(nd) { var t = nd.x; nd.x = nd.y; nd.y = t; });

        var x0=Infinity, x1=-Infinity, y0=Infinity, y1=-Infinity;
        root.each(function(nd) {
          var rw = textWidth(nd.data.name);
          if (nd.x < x0) x0 = nd.x;
          if (nd.x + rw > x1) x1 = nd.x + rw;
          if (nd.y - 19 < y0) y0 = nd.y - 19;
          if (nd.y + 19 > y1) y1 = nd.y + 19;
        });
        var pad = 24; x0 -= pad; y0 -= pad; x1 += pad; y1 += pad;
        var vw = x1 - x0, vh = y1 - y0;

        var svg = d3.select(container).append('svg')
          .attr('viewBox', x0+' '+y0+' '+vw+' '+vh)
          .style('width','100%').style('height','auto')
          .style('min-height','300px').style('max-height','700px')
          .style('cursor','grab');

        var g = svg.append('g');
        var zoom = d3.zoom().scaleExtent([0.3, 3])
          .on('zoom', function(ev) { g.attr('transform', ev.transform); svg.style('cursor','grabbing'); })
          .on('end', function() { svg.style('cursor','grab'); });
        svg.call(zoom);

        /* Horizontal bezier: parent right edge -> child left edge */
        g.selectAll('.link').data(root.links()).join('path')
          .attr('fill','none').attr('stroke','#aaa').attr('stroke-width', 1.5)
          .attr('d', function(d) {
            var sx = d.source.x + textWidth(d.source.data.name), sy = d.source.y;
            var tx = d.target.x, ty = d.target.y;
            var mx = (sx + tx) / 2;
            return 'M'+sx+','+sy+'C'+mx+','+sy+' '+mx+','+ty+' '+tx+','+ty;
          });

        var nodeMap = {};
        var node = g.selectAll('.node').data(root.descendants()).join('g')
          .attr('transform', function(nd) { return 'translate('+nd.x+','+nd.y+')'; });

        node.append('rect').attr('rx',5).attr('ry',5)
          .attr('x', 0).attr('y', -15)
          .attr('width', function(nd) { return textWidth(nd.data.name); })
          .attr('height', 30)
          .attr('fill', isAST ? '#fde8e8' : '#e8f0fd')
          .attr('stroke', isAST ? '#8b1a1a' : '#2c5aa0')
          .attr('stroke-width', 1.5)
          .style('filter','drop-shadow(0 1px 2px rgba(0,0,0,0.08))');

        node.append('text')
          .attr('x', 10).attr('dy','0.35em').attr('text-anchor','start')
          .attr('font-size','13px').attr('font-family','Consolas,monospace')
          .attr('fill', isAST ? '#8b1a1a' : '#333')
          .text(function(nd) { return nd.data.name; });

        node.each(function(nd) { nodeMap[nd.data.id] = d3.select(this); });
        return nodeMap;
      }

      var d3Cst = renderD3Tree(P+'-d3-cst', o.cstData, false);
      var d3Ast = renderD3Tree(P+'-d3-ast', o.astData, true);

      /* Hide D3 AST initially */
      AI.forEach(function(id) { if (d3Ast[id]) d3Ast[id].attr('opacity', 0); });
      d3.select('#'+P+'-d3-ast').selectAll('path').attr('opacity', 0);

      // ===== CSS Tree init =====
      AI.forEach(function(id) {
        var el = document.getElementById(P+'-css-'+id);
        if (el) {
          el.style.opacity = '0';
          el.parentElement.style.maxHeight = '0';
          el.parentElement.style.overflow = 'hidden';
          el.parentElement.style.transition = 'all 0.4s';
        }
      });

      // ===== Tab switching =====
      var tabD3 = document.getElementById(P+'-tab-d3');
      var tabCSS = document.getElementById(P+'-tab-css');
      var viewD3 = document.getElementById(P+'-view-d3');
      var viewCSS = document.getElementById(P+'-view-css');
      tabD3.onclick = function() {
        tabD3.classList.add('active'); tabCSS.classList.remove('active');
        viewD3.style.display = ''; viewCSS.style.display = 'none';
      };
      tabCSS.onclick = function() {
        tabCSS.classList.add('active'); tabD3.classList.remove('active');
        viewCSS.style.display = ''; viewD3.style.display = 'none';
      };

      // ===== Apply functions =====
      function applyD3(idx) {
        var step = steps[idx];
        CI.forEach(function(id) {
          var g = d3Cst[id]; if (!g) return;
          var s = S[step.c[id] || 'N'];
          g.select('rect').attr('fill', s.bg === 'transparent' ? '#fff' : s.bg).attr('stroke', s.c);
          g.select('text').attr('fill', s.c);
          g.attr('opacity', s.o);
        });
        var astG = d3.select('#'+P+'-d3-ast').select('svg g');
        astG.selectAll('path').attr('opacity', function(d) {
          var tgt = d.target && d.target.data ? d.target.data.id : null;
          return (tgt && step.a[tgt]) ? 1 : 0;
        });
        AI.forEach(function(id) {
          var g = d3Ast[id]; if (!g) return;
          var k = step.a[id];
          if (!k) { g.attr('opacity', 0); return; }
          var s = S[k];
          g.attr('opacity', s.o);
          g.select('rect').attr('fill', s.bg === 'transparent' ? '#fff' : s.bg).attr('stroke', s.c);
          g.select('text').attr('fill', s.c);
        });
      }

      function applyCSS(idx) {
        var step = steps[idx];
        CI.forEach(function(id) {
          var el = document.getElementById(P+'-css-'+id); if (!el) return;
          var s = S[step.c[id] || 'N'];
          el.style.background = s.bg; el.style.color = s.c; el.style.opacity = s.o;
        });
        AI.forEach(function(id) {
          var el = document.getElementById(P+'-css-'+id); if (!el) return;
          var li = el.parentElement;
          var k = step.a[id];
          if (!k) { el.style.opacity='0'; li.style.maxHeight='0'; li.style.overflow='hidden'; return; }
          var s = S[k];
          el.style.background = s.bg; el.style.color = s.c; el.style.opacity = s.o;
          li.style.maxHeight = '30px'; li.style.overflow = 'visible';
        });
      }

      // ===== Stepper =====
      var cur = 0;
      function apply(idx) {
        applyD3(idx);
        applyCSS(idx);
        document.getElementById(o.info).textContent = steps[idx].d;
        document.getElementById(o.val).textContent = '\uB2E8\uACC4 ' + idx + ' / ' + (steps.length - 1);
        document.getElementById(o.prev).disabled = idx === 0;
        document.getElementById(o.next).disabled = idx === steps.length - 1;
      }
      document.getElementById(o.prev).onclick = function() { if (cur > 0) { cur--; apply(cur); } };
      document.getElementById(o.next).onclick = function() { if (cur < steps.length - 1) { cur++; apply(cur); } };
      apply(0);
    }

    return '(' + runtime.toString() + ')(' + JSON.stringify(cfg) + ');';
  }
};

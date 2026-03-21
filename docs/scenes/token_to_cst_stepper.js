// token_to_cst_stepper.js — Canvas2D scene plugin for Section 7
// 토큰 스트림 → CST 조립 과정을 단계별로 보여주는 인터랙티브 스테퍼
// 왼쪽: 토큰 스트림 (소비 하이라이트), 오른쪽: CST 트리 (D3/CSS 탭)
// Type: canvas2d | Section 7 of TokenStream_to_CST_설명

module.exports = {
  type: 'canvas2d',

  html: function(ids) {
    var P = ids.canvas;

    // --- CSS tree helper ---
    function li(id, label, ch) {
      var inner = ch ? '<ul>' + ch.join('') + '</ul>' : '';
      return '<li><span class="s7-node" id="' + P + '-css-' + id + '">' +
        label + '</span>' + inner + '</li>';
    }

    var cssTree = '<ul class="s7-tree">' + li('n0', 'messageDef', [
      li('n1', 'MESSAGE'),
      li('n2', 'messageName', [li('n3', 'ID("Player")')]),
      li('n4', 'messageBody', [
        li('n5', 'LC'),
        li('n6', 'field[0]', [
          li('n7', 'type_: STRING'), li('n8', 'fieldName: ID("name")'),
          li('n9', 'EQ'), li('n10', 'fieldNumber: INT_LIT("1")'), li('n11', 'SEMI')
        ]),
        li('n12', 'field[1]', [
          li('n13', 'type_: INT32'), li('n14', 'fieldName: ID("level")'),
          li('n15', 'EQ'), li('n16', 'fieldNumber: INT_LIT("2")'), li('n17', 'SEMI')
        ]),
        li('n18', 'RC')
      ])
    ]) + '</ul>';

    // --- Token list HTML ---
    var tokens = [
      ['t0', 'MESSAGE', '"message"', 'kw'],
      ['t1', 'IDENTIFIER', '"Player"', 'id'],
      ['t2', 'LC', '"{"', 'sym'],
      ['t3', 'STRING', '"string"', 'kw'],
      ['t4', 'IDENTIFIER', '"name"', 'id'],
      ['t5', 'EQ', '"="', 'sym'],
      ['t6', 'INT_LIT', '"1"', 'lit'],
      ['t7', 'SEMI', '";"', 'sym'],
      ['t8', 'INT32', '"int32"', 'kw'],
      ['t9', 'IDENTIFIER', '"level"', 'id'],
      ['t10', 'EQ', '"="', 'sym'],
      ['t11', 'INT_LIT', '"2"', 'lit'],
      ['t12', 'SEMI', '";"', 'sym'],
      ['t13', 'RC', '"}"', 'sym']
    ];
    var tokenHtml = '';
    for (var i = 0; i < tokens.length; i++) {
      var t = tokens[i];
      tokenHtml += '<div class="s7-tok" id="' + P + '-' + t[0] + '">' +
        '<span class="s7-tok-idx">' + t[0] + '</span>' +
        '<span class="s7-tok-type">' + t[1] + '</span>' +
        '<span class="s7-tok-val">' + t[2] + '</span>' +
        '</div>';
    }

    // --- Scoped styles ---
    var style = '<style>\n' +
      '.s7-wrap{display:flex;gap:12px;flex-wrap:wrap;}\n' +
      '.s7-tok-panel{flex:0 0 220px;min-width:200px;max-width:240px;background:#f8f9fa;border:1px solid #e0e0e0;border-radius:6px;padding:8px 10px;align-self:flex-start;}\n' +
      '.s7-tok-title{font-weight:bold;text-align:center;color:#2c5aa0;margin-bottom:8px;font-size:0.85em;}\n' +
      '.s7-tok{display:flex;gap:4px;align-items:center;padding:3px 6px;border-radius:4px;margin:2px 0;' +
        'font-family:Consolas,monospace;font-size:0.76em;transition:all 0.3s;border:1px solid transparent;}\n' +
      '.s7-tok-idx{color:#999;min-width:22px;font-size:0.85em;}\n' +
      '.s7-tok-type{font-weight:bold;flex:1;}\n' +
      '.s7-tok-val{color:#666;font-size:0.9em;}\n' +
      '.s7-cst-panel{flex:1 1 400px;min-width:300px;}\n' +
      '.s7-cst-title{font-weight:bold;text-align:center;color:#2c5aa0;margin-bottom:6px;font-size:0.85em;}\n' +
      '.s7-tabs{display:flex;gap:0;margin-bottom:0;}\n' +
      '.s7-tab{padding:6px 16px;border:1px solid #ddd;border-bottom:none;border-radius:6px 6px 0 0;' +
        'background:#f5f5f5;color:#666;cursor:pointer;font-size:0.82em;font-weight:bold;transition:all 0.2s;}\n' +
      '.s7-tab.active{background:#fff;color:#2c5aa0;border-color:#ccc;position:relative;z-index:1;margin-bottom:-1px;padding-bottom:7px;}\n' +
      '.s7-tab:not(.active):hover{background:#eef3fa;color:#2c5aa0;}\n' +
      '.s7-view{border:1px solid #ccc;border-radius:0 6px 6px 6px;padding:12px;background:#fff;}\n' +
      '.s7-d3-box{border:1px solid #e0e0e0;border-radius:6px;overflow:hidden;min-height:120px;}\n' +
      '.s7-d3-box svg{display:block;width:100%;height:auto;min-height:380px;max-height:700px;cursor:grab;}\n' +
      '.s7-tree,.s7-tree ul{list-style:none;margin:0;padding-left:0;}\n' +
      '.s7-tree ul{padding-left:20px;}\n' +
      '.s7-tree li{position:relative;padding:1px 0;}\n' +
      ".s7-tree li::before{content:'';position:absolute;left:-13px;top:0;height:100%;border-left:1.5px solid #bbb;}\n" +
      ".s7-tree li::after{content:'';position:absolute;left:-13px;top:13px;width:11px;border-top:1.5px solid #bbb;}\n" +
      '.s7-tree li:last-child::before{height:13px;}\n' +
      '.s7-tree>li::before,.s7-tree>li::after{display:none;}\n' +
      ".s7-node{display:inline-block;padding:2px 7px;border-radius:4px;font-family:Consolas,'Courier New',monospace;" +
        'font-size:0.76em;transition:background 0.3s,color 0.3s,opacity 0.3s;}\n' +
      '</style>\n';

    // --- Token panel ---
    var tokPanel =
      '<div class="s7-tok-panel">' +
        '<div class="s7-tok-title">\uD1A0\uD070 \uC2A4\uD2B8\uB9BC (14\uAC1C)</div>' +
        tokenHtml +
      '</div>';

    // --- Tabs ---
    var tabsHtml =
      '<div class="s7-tabs">' +
        '<button class="s7-tab active" id="' + P + '-tab-d3">D3.js \uD2B8\uB9AC (SVG)</button>' +
        '<button class="s7-tab" id="' + P + '-tab-css">CSS \uD2B8\uB9AC (HTML)</button>' +
      '</div>';

    // --- D3 view ---
    var d3View =
      '<div class="s7-view" id="' + P + '-view-d3">' +
        '<div class="s7-d3-box" id="' + P + '-d3-cst"></div>' +
      '</div>';

    // --- CSS view ---
    var cssView =
      '<div class="s7-view" id="' + P + '-view-css" style="display:none;text-align:left;">' +
        cssTree +
      '</div>';

    // --- CST panel ---
    var cstPanel =
      '<div class="s7-cst-panel">' +
        '<div class="s7-cst-title">CST (Parse Tree) \u2014 19\uAC1C \uB178\uB4DC</div>' +
        tabsHtml + d3View + cssView +
      '</div>';

    // --- Info box ---
    var infoBox =
      '<div id="' + ids.info + '" style="text-align:center;padding:10px 16px;background:#e8f4fd;' +
        'border-left:4px solid #5ba3d9;border-radius:0 6px 6px 0;margin-bottom:10px;' +
        'font-size:0.88em;color:#333;min-height:2.5em;display:flex;align-items:center;' +
        'justify-content:center;">\uCD08\uAE30 \uC0C1\uD0DC \u2014 \uD30C\uC11C \uC2DC\uC791 \uC804</div>';

    // --- Stepper controls ---
    var controls =
      '<div class="stepper-controls">' +
        '<button class="stepper-btn" id="' + ids.prev + '" disabled>\u2190 \uC774\uC804</button>' +
        '<span class="stepper-label" id="' + ids.val + '">\uB2E8\uACC4 0 / 12</span>' +
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
        dot('#cce5ff', '#004085', '\uC18C\uBE44 \uC911 (\uD1A0\uD070)') +
        dot('#d4edda', '#155724', '\uC0DD\uC131 \uC911 (CST)') +
        dot('#e2e3e5', '#6c757d', '\uC18C\uBE44 \uC644\uB8CC') +
        dot('#fff3cd', '#856404', '\uD65C\uC131 \uB178\uB4DC') +
      '</div>';

    return style +
      '<div class="s7-wrap">' + tokPanel + cstPanel + '</div>' +
      infoBox + controls + legend;
  },

  build: function(ids) {
    var nodeIds = ['n0','n1','n2','n3','n4','n5','n6','n7','n8','n9',
                   'n10','n11','n12','n13','n14','n15','n16','n17','n18'];
    var tokIds = ['t0','t1','t2','t3','t4','t5','t6','t7','t8','t9',
                  't10','t11','t12','t13'];

    // Token categories for coloring
    var tokCat = {
      t0:'kw', t1:'id', t2:'sym', t3:'kw', t4:'id', t5:'sym', t6:'lit', t7:'sym',
      t8:'kw', t9:'id', t10:'sym', t11:'lit', t12:'sym', t13:'sym'
    };

    // CST node states: N=normal(hidden), G=new(green), A=active(yellow), S=stable, H=hidden(default)
    // Token states: P=pending, C=consuming(blue), D=done(gray)
    var steps = [
      { d: '\uCD08\uAE30 \uC0C1\uD0DC \u2014 14\uAC1C \uD1A0\uD070\uC774 \uD30C\uC11C\uB97C \uAE30\uB2E4\uB9BD\uB2C8\uB2E4.',
        done: 0, cur: [], c: {} },
      { d: 'messageDef() \uC9C4\uC785 \u2014 \uCD5C\uC0C1\uC704 \uADDC\uCE59 Context \uB178\uB4DC \uC0DD\uC131.',
        done: 0, cur: [], c: { n0:'G' } },
      { d: 'Match(MESSAGE) \u2014 t0 \uC18C\uBE44, TerminalNode \uCD94\uAC00.',
        done: 0, cur: [0], c: { n0:'A', n1:'G' } },
      { d: 'messageName() \u2192 ident() \u2192 Match(IDENTIFIER) \u2014 t1 \uC18C\uBE44.',
        done: 1, cur: [1], c: { n0:'A', n1:'S', n2:'G', n3:'G' } },
      { d: 'messageBody() \uC9C4\uC785, Match(LC) \u2014 t2 \uC18C\uBE44.',
        done: 2, cur: [2], c: { n0:'S', n1:'S', n2:'S', n3:'S', n4:'G', n5:'G' } },
      { d: 'field() \uC9C4\uC785, type_() \u2192 Match(STRING) \u2014 t3 \uC18C\uBE44.',
        done: 3, cur: [3], c: { n0:'S', n1:'S', n2:'S', n3:'S', n4:'A', n5:'S', n6:'G', n7:'G' } },
      { d: 'fieldName() \u2192 Match(ID), Match(EQ) \u2014 t4, t5 \uB3D9\uC2DC \uC18C\uBE44.',
        done: 4, cur: [4, 5], c: { n0:'S', n1:'S', n2:'S', n3:'S', n4:'A', n5:'S', n6:'A', n7:'S', n8:'G', n9:'G' } },
      { d: 'fieldNumber() \u2192 Match(INT_LIT) \u2014 t6 \uC18C\uBE44.',
        done: 6, cur: [6], c: { n0:'S', n1:'S', n2:'S', n3:'S', n4:'A', n5:'S', n6:'A', n7:'S', n8:'S', n9:'S', n10:'G' } },
      { d: 'Match(SEMI) \u2014 t7 \uC18C\uBE44. field[0] \uC644\uC131, ExitRule().',
        done: 7, cur: [7], c: { n0:'S', n1:'S', n2:'S', n3:'S', n4:'A', n5:'S', n6:'S', n7:'S', n8:'S', n9:'S', n10:'S', n11:'G' } },
      { d: '\uB2E4\uC74C field() \uC9C4\uC785, type_() \u2192 Match(INT32) \u2014 t8 \uC18C\uBE44.',
        done: 8, cur: [8], c: { n0:'S', n1:'S', n2:'S', n3:'S', n4:'A', n5:'S', n6:'S', n7:'S', n8:'S', n9:'S', n10:'S', n11:'S', n12:'G', n13:'G' } },
      { d: 'fieldName() \u2192 Match(ID), Match(EQ) \u2014 t9, t10 \uB3D9\uC2DC \uC18C\uBE44.',
        done: 9, cur: [9, 10], c: { n0:'S', n1:'S', n2:'S', n3:'S', n4:'A', n5:'S', n6:'S', n7:'S', n8:'S', n9:'S', n10:'S', n11:'S', n12:'A', n13:'S', n14:'G', n15:'G' } },
      { d: 'fieldNumber() \u2192 Match(INT_LIT) \u2014 t11 \uC18C\uBE44.',
        done: 11, cur: [11], c: { n0:'S', n1:'S', n2:'S', n3:'S', n4:'A', n5:'S', n6:'S', n7:'S', n8:'S', n9:'S', n10:'S', n11:'S', n12:'A', n13:'S', n14:'S', n15:'S', n16:'G' } },
      { d: 'Match(SEMI), Match(RC) \u2014 t12, t13 \uC18C\uBE44. \uC804\uCCB4 CST \uC644\uC131!',
        done: 12, cur: [12, 13], c: { n0:'S', n1:'S', n2:'S', n3:'S', n4:'S', n5:'S', n6:'S', n7:'S', n8:'S', n9:'S', n10:'S', n11:'S', n12:'S', n13:'S', n14:'S', n15:'S', n16:'S', n17:'G', n18:'G' } }
    ];

    var cstData = {
      id:'n0', name:'messageDef', children:[
        {id:'n1', name:'MESSAGE'},
        {id:'n2', name:'messageName', children:[
          {id:'n3', name:'ID("Player")'}
        ]},
        {id:'n4', name:'messageBody', children:[
          {id:'n5', name:'LC'},
          {id:'n6', name:'field[0]', children:[
            {id:'n7', name:'type_: STRING'},
            {id:'n8', name:'fieldName: ID("name")'},
            {id:'n9', name:'EQ'},
            {id:'n10', name:'fieldNumber: INT_LIT("1")'},
            {id:'n11', name:'SEMI'}
          ]},
          {id:'n12', name:'field[1]', children:[
            {id:'n13', name:'type_: INT32'},
            {id:'n14', name:'fieldName: ID("level")'},
            {id:'n15', name:'EQ'},
            {id:'n16', name:'fieldNumber: INT_LIT("2")'},
            {id:'n17', name:'SEMI'}
          ]},
          {id:'n18', name:'RC'}
        ]}
      ]
    };

    var cfg = {
      P: ids.canvas,
      info: ids.info,
      val: ids.val,
      prev: ids.prev,
      next: ids.next,
      steps: steps,
      NI: nodeIds,
      TI: tokIds,
      TC: tokCat,
      cstData: cstData
    };

    // Runtime function — serialized via toString(), executed in browser
    function runtime(o) {
      var P=o.P, steps=o.steps, NI=o.NI, TI=o.TI, TC=o.TC;

      // === Token color by category ===
      var catColors = {
        kw:  { pending:'#2c5aa0', consuming:'#fff',    done:'#999' },
        id:  { pending:'#1a7a4c', consuming:'#fff',    done:'#999' },
        sym: { pending:'#b45309', consuming:'#fff',    done:'#999' },
        lit: { pending:'#7c3aed', consuming:'#fff',    done:'#999' }
      };
      var catBg = {
        pending: 'transparent', consuming: '#3b82f6', done: '#f0f0f0'
      };

      // === CST node styles ===
      var nodeStyles = {
        G: { bg: '#d4edda', c: '#155724', o: 1, stroke: '#28a745' },   // new/green
        A: { bg: '#fff3cd', c: '#856404', o: 1, stroke: '#ffc107' },   // active/yellow
        S: { bg: '#e8f0fd', c: '#2c5aa0', o: 1, stroke: '#2c5aa0' },  // stable/blue
        H: { bg: 'transparent', c: '#333', o: 0, stroke: '#ccc' }      // hidden
      };

      // === D3.js Horizontal Tree ===
      function textWidth(l) { return l.length * 10.5 + 28; }

      function renderD3Tree(containerId, treeData) {
        var container = document.getElementById(containerId);
        var root = d3.hierarchy(treeData);
        var layout = d3.tree().nodeSize([48, 260])
          .separation(function(a, b) { return a.parent === b.parent ? 1 : 1.2; });
        layout(root);
        root.each(function(nd) { var t = nd.x; nd.x = nd.y; nd.y = t; });

        var x0=Infinity, x1=-Infinity, y0=Infinity, y1=-Infinity;
        root.each(function(nd) {
          var rw = textWidth(nd.data.name);
          if (nd.x < x0) x0 = nd.x;
          if (nd.x + rw > x1) x1 = nd.x + rw;
          if (nd.y - 22 < y0) y0 = nd.y - 22;
          if (nd.y + 22 > y1) y1 = nd.y + 22;
        });
        var pad = 20; x0 -= pad; y0 -= pad; x1 += pad; y1 += pad;

        var svg = d3.select(container).append('svg')
          .attr('viewBox', x0+' '+y0+' '+(x1-x0)+' '+(y1-y0))
          .style('width','100%').style('height','auto')
          .style('min-height','380px').style('max-height','700px')
          .style('cursor','grab');

        var g = svg.append('g');
        var zoom = d3.zoom().scaleExtent([0.3, 3])
          .on('zoom', function(ev) { g.attr('transform', ev.transform); svg.style('cursor','grabbing'); })
          .on('end', function() { svg.style('cursor','grab'); });
        svg.call(zoom);

        /* links */
        var linkSel = g.selectAll('.link').data(root.links()).join('path')
          .attr('fill','none').attr('stroke','#aaa').attr('stroke-width', 1.5).attr('opacity', 0)
          .attr('d', function(d) {
            var sx = d.source.x + textWidth(d.source.data.name), sy = d.source.y;
            var tx = d.target.x, ty = d.target.y;
            var mx = (sx + tx) / 2;
            return 'M'+sx+','+sy+'C'+mx+','+sy+' '+mx+','+ty+' '+tx+','+ty;
          });

        var nodeMap = {};
        var node = g.selectAll('.node').data(root.descendants()).join('g')
          .attr('transform', function(nd) { return 'translate('+nd.x+','+nd.y+')'; })
          .attr('opacity', 0);

        node.append('rect').attr('rx',5).attr('ry',5)
          .attr('x', 0).attr('y', -18)
          .attr('width', function(nd) { return textWidth(nd.data.name); })
          .attr('height', 36)
          .attr('fill', '#e8f0fd')
          .attr('stroke', '#2c5aa0')
          .attr('stroke-width', 1.5)
          .style('filter','drop-shadow(0 1px 2px rgba(0,0,0,0.08))');

        node.append('text')
          .attr('x', 8).attr('dy','0.35em').attr('text-anchor','start')
          .attr('font-size','17px').attr('font-family','Consolas,monospace')
          .attr('fill', '#333')
          .text(function(nd) { return nd.data.name; });

        node.each(function(nd) { nodeMap[nd.data.id] = d3.select(this); });
        return { nodeMap: nodeMap, linkSel: linkSel };
      }

      var d3Result = renderD3Tree(P+'-d3-cst', o.cstData);
      var d3Nodes = d3Result.nodeMap;
      var d3Links = d3Result.linkSel;

      // === CSS Tree init — all hidden ===
      NI.forEach(function(id) {
        var el = document.getElementById(P+'-css-'+id);
        if (el) {
          el.style.opacity = '0';
          el.parentElement.style.maxHeight = '0';
          el.parentElement.style.overflow = 'hidden';
          el.parentElement.style.transition = 'all 0.4s';
        }
      });

      // === Tab switching ===
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

      // === Apply token states ===
      function applyTokens(step) {
        var done = step.done;
        var curSet = {};
        for (var i = 0; i < step.cur.length; i++) curSet[step.cur[i]] = true;

        TI.forEach(function(tid, idx) {
          var el = document.getElementById(P+'-'+tid);
          if (!el) return;
          var state;
          if (curSet[idx]) state = 'consuming';
          else if (idx < done) state = 'done';
          else state = 'pending';

          var cat = TC[tid];
          var cc = catColors[cat];
          el.style.background = catBg[state];
          el.style.color = cc[state];
          el.style.borderColor = state === 'consuming' ? '#3b82f6' : 'transparent';
          el.style.fontWeight = state === 'consuming' ? 'bold' : 'normal';
          if (state === 'done') {
            el.style.textDecoration = 'line-through';
            el.style.opacity = '0.55';
          } else {
            el.style.textDecoration = 'none';
            el.style.opacity = '1';
          }
        });
      }

      // === Apply CST D3 ===
      function applyD3(step) {
        NI.forEach(function(id) {
          var g = d3Nodes[id]; if (!g) return;
          var k = step.c[id] || 'H';
          var s = nodeStyles[k];
          g.transition().duration(300)
            .attr('opacity', s.o);
          g.select('rect').transition().duration(300)
            .attr('fill', s.bg === 'transparent' ? '#fff' : s.bg)
            .attr('stroke', s.stroke);
          g.select('text').transition().duration(300)
            .attr('fill', s.c);
        });
        d3Links.transition().duration(300).attr('opacity', function(d) {
          var tgt = d.target && d.target.data ? d.target.data.id : null;
          return (tgt && step.c[tgt]) ? 1 : 0;
        });
      }

      // === Apply CST CSS ===
      function applyCSS(step) {
        NI.forEach(function(id) {
          var el = document.getElementById(P+'-css-'+id); if (!el) return;
          var li = el.parentElement;
          var k = step.c[id] || 'H';
          var s = nodeStyles[k];
          if (k === 'H') {
            el.style.opacity = '0';
            li.style.maxHeight = '0';
            li.style.overflow = 'hidden';
          } else {
            el.style.opacity = s.o;
            el.style.background = s.bg;
            el.style.color = s.c;
            li.style.maxHeight = '28px';
            li.style.overflow = 'visible';
          }
        });
      }

      // === Stepper ===
      var cur = 0;
      function apply(idx) {
        var step = steps[idx];
        applyTokens(step);
        applyD3(step);
        applyCSS(step);
        document.getElementById(o.info).textContent = step.d;
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

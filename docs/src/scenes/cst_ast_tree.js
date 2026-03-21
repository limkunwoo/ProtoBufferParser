// Scene plugin: D3.js interactive tree — CST vs AST comparison
// Type: d3tree | Section 2 of CST_to_AST_설명
// Usage: loaded by build_qna_html.js via loadScene('cst_ast_tree')

module.exports = {
  type: 'd3tree',

  colors: {
    rule:       { bg: '#e8f0fd', border: '#2c5aa0', label: 'Rule (non-terminal)' },
    token:      { bg: '#fef9e7', border: '#b07d00', label: 'Token (terminal)' },
    punct:      { bg: '#f0f0f0', border: '#999999', label: 'Punctuation' },
    ast:        { bg: '#fde8e8', border: '#8b1a1a', label: 'AST Node' },
    'ast-leaf': { bg: '#fff5e6', border: '#7a5200', label: 'AST Leaf (value)' }
  },

  trees: [
    {
      title: 'CST (Concrete Syntax Tree)',
      data: {
        name: 'field', cat: 'rule',
        children: [
          { name: 'fieldLabel', cat: 'rule', children: [
            { name: 'REPEATED', cat: 'token' }
          ]},
          { name: 'type_', cat: 'rule', children: [
            { name: 'STRING', cat: 'token' }
          ]},
          { name: 'fieldName', cat: 'rule', children: [
            { name: 'ident', cat: 'rule', children: [
              { name: 'ID("name")', cat: 'token' }
            ]}
          ]},
          { name: 'EQ', cat: 'punct' },
          { name: 'fieldNumber', cat: 'rule', children: [
            { name: 'intLit', cat: 'rule', children: [
              { name: 'INT_LIT("1")', cat: 'token' }
            ]}
          ]},
          { name: 'SEMI', cat: 'punct' }
        ]
      }
    },
    {
      title: 'AST (Abstract Syntax Tree)',
      data: {
        name: 'FieldNode', cat: 'ast',
        children: [
          { name: 'label: REPEATED', cat: 'ast-leaf' },
          { name: 'type: "string"', cat: 'ast-leaf' },
          { name: 'name: "name"', cat: 'ast-leaf' },
          { name: 'number: 1', cat: 'ast-leaf' }
        ]
      }
    }
  ],

  /**
   * Returns the HTML skeleton for the two-panel tree view + legend.
   * @param {object} ids - { wrapper, panel0, panel1, tip0, tip1, legend }
   */
  html: function(ids) {
    var legendItems = Object.keys(this.colors).map(function(key) {
      var c = this.colors[key];
      return '<span style="display:inline-flex;align-items:center;gap:4px;margin-right:12px;">' +
        '<span style="display:inline-block;width:14px;height:14px;border-radius:3px;' +
        'background:' + c.bg + ';border:2px solid ' + c.border + ';"></span>' +
        '<span style="font-size:0.82em;color:#555;">' + c.label + '</span></span>';
    }.bind(this)).join('');

    var panelsHtml = this.trees.map(function(t, i) {
      var panelId = i === 0 ? ids.panel0 : ids.panel1;
      var tipId   = i === 0 ? ids.tip0   : ids.tip1;
      return '<div class="d3tree-panel">' +
        '<div style="font-weight:bold;text-align:center;margin-bottom:6px;color:#2c5aa0;">' +
          t.title + '</div>' +
        '<div id="' + panelId + '" class="d3tree-container"></div>' +
        '<div id="' + tipId + '" class="d3tree-tip"></div>' +
        '</div>';
    }).join('\n');

    return '<div id="' + ids.wrapper + '" class="d3tree-wrapper">' +
      '<div class="d3tree-pair">' + panelsHtml + '</div>' +
      '<div id="' + ids.legend + '" class="d3tree-legend">' + legendItems + '</div>' +
      '</div>';
  },

  /**
   * Returns JS code that calls D3Tree.render() for each panel.
   * Executed inside an IIFE in the final HTML.
   * @param {object} ids - same as html()
   */
  build: function(ids) {
    var colorsJson = JSON.stringify(this.colors);
    var blocks = this.trees.map(function(t, i) {
      var panelId = i === 0 ? ids.panel0 : ids.panel1;
      var tipId   = i === 0 ? ids.tip0   : ids.tip1;
      var dataJson = JSON.stringify(t.data);
      return 'D3Tree.render(' +
        JSON.stringify(panelId) + ', ' +
        JSON.stringify(tipId) + ', ' +
        dataJson + ', colors);';
    }).join('\n');

    return 'var colors = ' + colorsJson + ';\n' + blocks;
  }
};

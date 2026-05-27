import{g as i}from"./index-BL7Bold5.js";function r(e){const a=new URLSearchParams;return e.search&&a.set("search",e.search),e.page!==void 0&&a.set("page",String(e.page)),e.pageSize!==void 0&&a.set("pageSize",String(e.pageSize)),a.size>0?`?${a.toString()}`:""}const t={list:(e={})=>i(`/api/members${r(e)}`),get:e=>i(`/api/members/${e}`)};export{t as m};
//# sourceMappingURL=members-7MPPidW6.js.map

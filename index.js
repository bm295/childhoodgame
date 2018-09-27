const express = require('express')
const app = express()
const homeController = require('./controller/home-controller')

app.set('port', (process.env.PORT || 5000))
app.use(express.static(__dirname + '/public'))
app.set("view engine", "ejs")
app.set("views", "./views")

app.get('/', homeController.getHomeView)

app.listen(app.get('port'), function() {
  console.log("Node app is running at localhost:" + app.get('port'))
})
